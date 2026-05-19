using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class AtualizarOrcamentoCommandHandler : ICommandHandler<AtualizarOrcamentoCommand>
{
    private readonly IOrcamentoRepository _repo;
    private readonly IProcedimentoCirurgicoRepository _procedimentoRepo;
    private readonly IItemInventarioRepository _inventarioRepo;
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IConfiguracaoLocalCirurgiaRepository _configLocalRepo;

    public AtualizarOrcamentoCommandHandler(
        IOrcamentoRepository repo,
        IProcedimentoCirurgicoRepository procedimentoRepo,
        IItemInventarioRepository inventarioRepo,
        IAgendamentoRepository agendamentoRepo,
        IConfiguracaoLocalCirurgiaRepository configLocalRepo)
    {
        _repo = repo;
        _procedimentoRepo = procedimentoRepo;
        _inventarioRepo = inventarioRepo;
        _agendamentoRepo = agendamentoRepo;
        _configLocalRepo = configLocalRepo;
    }

    public async Task Handle(AtualizarOrcamentoCommand cmd)
    {
        var orcamento = await _repo.ObterPorIdCompletoOuNulo(cmd.OrcamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Orçamento não encontrado.");

        if (cmd.ProcedimentoCirurgicoId is { } procId)
        {
            var proc = await _procedimentoRepo.ObterPorIdOuNulo(procId, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Procedimento cirúrgico não encontrado.");
            if (proc.PacienteId != orcamento.PacienteId)
                throw new BusinessException("Procedimento cirúrgico não pertence ao paciente neste estabelecimento.");
        }

        if (cmd.AgendamentoId is { } agId)
        {
            var ag = await _agendamentoRepo.ObterPorIdOuNulo(agId, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Agendamento não encontrado.");
            if (ag.PacienteId != orcamento.PacienteId)
                throw new BusinessException("Agendamento não pertence ao paciente neste estabelecimento.");
        }

        await ValidarImplantesCatalogo(cmd.Implantes, cmd.EstabelecimentoId);
        await ValidarCirurgiasCatalogo(cmd.Cirurgias, cmd.EstabelecimentoId, orcamento.PacienteId);

        var local = await MapearLocalAsync(cmd.LocalCirurgia, cmd.EstabelecimentoId);

        orcamento.Atualizar(
            cmd.Validade,
            cmd.Observacoes,
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
    }

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
}
