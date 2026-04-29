using Imedto.Backend.Contracts.Orcamentos;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class CriarOrcamentoCompletoCommandHandler : ICommandHandler<CriarOrcamentoCompletoCommand>
{
    private readonly IOrcamentoRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProcedimentoCirurgicoRepository _procedimentoRepo;
    private readonly IItemInventarioRepository _inventarioRepo;
    private readonly IEventBus _events;

    public CriarOrcamentoCompletoCommandHandler(
        IOrcamentoRepository repo,
        IPacienteRepository pacienteRepo,
        IProcedimentoCirurgicoRepository procedimentoRepo,
        IItemInventarioRepository inventarioRepo,
        IEventBus events)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _procedimentoRepo = procedimentoRepo;
        _inventarioRepo = inventarioRepo;
        _events = events;
    }

    public async Task Handle(CriarOrcamentoCompletoCommand cmd)
    {
        // Tenant guard — defesa contra cross-tenant via API.
        var paciente = await _pacienteRepo.ObterPorId(cmd.PacienteId);
        if (paciente.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");

        var tipo = OrcamentoCompletoMapping.ParseTipoOrcamento(cmd.Tipo);

        // Procedimento cirúrgico (quando informado) precisa pertencer ao mesmo estab + paciente.
        if (cmd.ProcedimentoCirurgicoId is { } procId)
        {
            var proc = await _procedimentoRepo.ObterPorId(procId);
            if (proc.EstabelecimentoId != cmd.EstabelecimentoId || proc.PacienteId != cmd.PacienteId)
                throw new BusinessException("Procedimento cirúrgico não pertence ao paciente neste estabelecimento.");
        }

        // Implantes do catálogo precisam pertencer ao estabelecimento.
        await ValidarImplantesCatalogo(cmd.Implantes, cmd.EstabelecimentoId);

        // Cirurgias com FK para o catálogo de procedimentos: validar tenant + paciente.
        await ValidarCirurgiasCatalogo(cmd.Cirurgias, cmd.EstabelecimentoId, cmd.PacienteId);

        var orcamento = Orcamento.CriarCompleto(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.Validade,
            cmd.Observacoes,
            cmd.CriadoPorUsuarioId,
            tipo,
            cmd.ProcedimentoCirurgicoId,
            OrcamentoCompletoMapping.MapConfiguracao(cmd.Configuracao),
            cmd.DescontoBruto,
            cmd.JurosBrutos,
            cmd.Itens.Select(i => new Orcamento.ItemPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)),
            cmd.Equipe.Select(e => new Orcamento.EquipePayload(e.ProfissionalUsuarioId, e.Papel, e.Valor)),
            cmd.Implantes.Select(i => new Orcamento.ImplantePayload(i.ItemInventarioId, i.Descricao, i.Quantidade, i.CustoUnitario)),
            cmd.FormasPagamento.Select(f => new Orcamento.FormaPagamentoPayload(
                f.FormaPagamentoId, f.Valor, f.Parcelas, f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao)),
            cmd.Cirurgias.Select(c => new Orcamento.CirurgiaPayload(
                c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade, c.DuracaoMinutos, c.ValorTotal)),
            OrcamentoCompletoMapping.MapInternacao(cmd.Internacao),
            OrcamentoCompletoMapping.MapAnestesia(cmd.Anestesia));

        await _repo.Salvar(orcamento);
        orcamento.DefinirNumero();
        await _repo.Salvar(orcamento);

        cmd.OrcamentoIdCriado = orcamento.Id;

        foreach (var ev in orcamento.DomainEvents)
            await _events.Publish(ev);
        orcamento.ClearDomainEvents();
    }

    private async Task ValidarImplantesCatalogo(
        IEnumerable<OrcamentoImplantePayload> implantes,
        long estabelecimentoId)
    {
        // Otimização: dedup por item para evitar N round-trips se o usuário repetir o mesmo item.
        var ids = implantes.Where(i => i.ItemInventarioId.HasValue)
                           .Select(i => i.ItemInventarioId!.Value)
                           .Distinct()
                           .ToList();
        foreach (var id in ids)
        {
            var item = await _inventarioRepo.ObterPorIdOuNulo(id)
                ?? throw new BusinessException($"Item de inventário {id} não encontrado.");
            if (item.EstabelecimentoId != estabelecimentoId)
                throw new BusinessException("Item de inventário não pertence a este estabelecimento.");
        }
    }

    private async Task ValidarCirurgiasCatalogo(
        IEnumerable<OrcamentoCirurgiaPayload> cirurgias,
        long estabelecimentoId,
        long pacienteId)
    {
        // Cirurgia vinculada ao catálogo precisa ser do mesmo estab + paciente que o orçamento.
        // Defesa contra cross-tenant: vide CLAUDE.md (multi-tenant é regra, não exceção).
        var ids = cirurgias.Where(c => c.ProcedimentoCirurgicoId.HasValue)
                           .Select(c => c.ProcedimentoCirurgicoId!.Value)
                           .Distinct()
                           .ToList();
        foreach (var id in ids)
        {
            var proc = await _procedimentoRepo.ObterPorId(id);
            if (proc.EstabelecimentoId != estabelecimentoId || proc.PacienteId != pacienteId)
                throw new BusinessException($"Procedimento cirúrgico {id} não pertence ao paciente neste estabelecimento.");
        }
    }
}
