using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class CriarOrcamentoCommandHandler : ICommandHandler<CriarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProcedimentoCirurgicoRepository _procedimentoRepo;
    private readonly IItemInventarioRepository _inventarioRepo;
    private readonly IFormaPagamentoRepository _formaRepo;
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IConfiguracaoLocalCirurgiaRepository _configLocalRepo;
    private readonly IEventBus _events;

    public CriarOrcamentoCommandHandler(
        IOrcamentoRepository repo,
        IPacienteRepository pacienteRepo,
        IProcedimentoCirurgicoRepository procedimentoRepo,
        IItemInventarioRepository inventarioRepo,
        IFormaPagamentoRepository formaRepo,
        IAgendamentoRepository agendamentoRepo,
        IConfiguracaoLocalCirurgiaRepository configLocalRepo,
        IEventBus events)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _procedimentoRepo = procedimentoRepo;
        _inventarioRepo = inventarioRepo;
        _formaRepo = formaRepo;
        _agendamentoRepo = agendamentoRepo;
        _configLocalRepo = configLocalRepo;
        _events = events;
    }

    public async Task Handle(CriarOrcamentoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        // Procedimento cirúrgico (raiz, opcional) precisa pertencer ao mesmo estab + paciente.
        if (cmd.ProcedimentoCirurgicoId is { } procId)
        {
            var proc = await _procedimentoRepo.ObterPorIdOuNulo(procId, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Procedimento cirúrgico não encontrado.");
            if (proc.PacienteId != cmd.PacienteId)
                throw new BusinessException("Procedimento cirúrgico não pertence ao paciente neste estabelecimento.");
        }

        // Agendamento opcional — valida pertencimento ao mesmo estab + paciente.
        if (cmd.AgendamentoId is { } agId)
        {
            var ag = await _agendamentoRepo.ObterPorIdOuNulo(agId, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Agendamento não encontrado.");
            if (ag.PacienteId != cmd.PacienteId)
                throw new BusinessException("Agendamento não pertence ao paciente neste estabelecimento.");
        }

        await ValidarImplantesCatalogo(cmd.Implantes, cmd.EstabelecimentoId);
        await ValidarCirurgiasCatalogo(cmd.Cirurgias, cmd.EstabelecimentoId, cmd.PacienteId);
        await ValidarFormasPagamentoCatalogo(cmd.FormasPagamento, cmd.EstabelecimentoId);

        var local = await MapearLocalAsync(cmd.LocalCirurgia, cmd.EstabelecimentoId);

        var orcamento = Orcamento.Criar(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.Validade,
            cmd.Observacoes,
            cmd.CriadoPorUsuarioId,
            cmd.ProcedimentoCirurgicoId,
            cmd.Itens.Select(i => new Orcamento.ItemPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)),
            cmd.Equipe.Select(e => new Orcamento.EquipePayload(e.ProfissionalUsuarioId, e.Papel, e.Valor)),
            cmd.Implantes.Select(i => new Orcamento.ImplantePayload(i.ItemInventarioId, i.Descricao, i.Quantidade, i.CustoUnitario)),
            cmd.FormasPagamento.Select(f => new Orcamento.FormaPagamentoPayload(
                f.FormaPagamentoId, f.Valor, f.Parcelas, f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao)),
            cmd.Cirurgias.Select(c => new Orcamento.CirurgiaPayload(
                c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade, c.DuracaoMinutos, c.ValorTotal)),
            local,
            OrcamentoMapping.MapAnestesia(cmd.Anestesia),
            titulo: cmd.Titulo,
            agendamentoId: cmd.AgendamentoId);

        await _repo.Salvar(orcamento);
        orcamento.DefinirNumero();
        await _repo.Salvar(orcamento);

        cmd.OrcamentoIdCriado = orcamento.Id;

        foreach (var ev in orcamento.DomainEvents)
            await _events.Publish(ev);
        orcamento.ClearDomainEvents();
    }

    /// <summary>
    /// Calcula <c>ValorCalculado</c> do local cirúrgico server-side a partir da
    /// configuração do estabelecimento — não confiamos no valor que o cliente envia.
    /// Se a configuração não existir para o tipo escolhido, lança BusinessException
    /// (assim o dono é forçado a configurar antes de cobrar).
    /// </summary>
    private async Task<Orcamento.LocalCirurgiaPayload?> MapearLocalAsync(
        OrcamentoLocalCirurgiaPayload? p, long estabelecimentoId)
    {
        if (p is null) return null;
        var tipo = OrcamentoMapping.ParseTipoLocal(p.Tipo);
        var config = await _configLocalRepo.ObterPorEstabelecimentoETipo(estabelecimentoId, tipo);
        if (config is null)
            throw new BusinessException("Local cirúrgico não configurado para este estabelecimento. Configure em Orçamento → Configurações.");
        var valor = OrcamentoCalculadora.CalcularValorLocal(tipo, p.TempoMinutos, config);
        return new Orcamento.LocalCirurgiaPayload(tipo, p.TempoMinutos, valor);
    }

    private async Task ValidarImplantesCatalogo(
        IEnumerable<OrcamentoImplantePayload> implantes,
        long estabelecimentoId)
    {
        var ids = implantes.Where(i => i.ItemInventarioId.HasValue)
                           .Select(i => i.ItemInventarioId!.Value)
                           .Distinct()
                           .ToList();
        foreach (var id in ids)
        {
            _ = await _inventarioRepo.ObterPorIdOuNulo(id, estabelecimentoId)
                ?? throw new BusinessException($"Item de inventário {id} não encontrado.");
        }
    }

    private async Task ValidarCirurgiasCatalogo(
        IEnumerable<OrcamentoCirurgiaPayload> cirurgias,
        long estabelecimentoId,
        long pacienteId)
    {
        var ids = cirurgias.Where(c => c.ProcedimentoCirurgicoId.HasValue)
                           .Select(c => c.ProcedimentoCirurgicoId!.Value)
                           .Distinct()
                           .ToList();
        foreach (var id in ids)
        {
            var proc = await _procedimentoRepo.ObterPorIdOuNulo(id, estabelecimentoId)
                ?? throw new BusinessException($"Procedimento cirúrgico {id} não encontrado.");
            if (proc.PacienteId != pacienteId)
                throw new BusinessException($"Procedimento cirúrgico {id} não pertence ao paciente neste estabelecimento.");
        }
    }

    private async Task ValidarFormasPagamentoCatalogo(
        IEnumerable<OrcamentoFormaPagamentoPayload> formas,
        long estabelecimentoId)
    {
        var ids = formas.Select(f => f.FormaPagamentoId).Distinct().ToList();
        foreach (var id in ids)
        {
            _ = await _formaRepo.ObterPorIdOuNulo(id, estabelecimentoId)
                ?? throw new BusinessException($"Forma de pagamento {id} não encontrada neste estabelecimento.");
        }
    }
}
