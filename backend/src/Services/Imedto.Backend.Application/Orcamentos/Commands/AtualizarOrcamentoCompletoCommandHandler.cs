using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

public class AtualizarOrcamentoCompletoCommandHandler : ICommandHandler<AtualizarOrcamentoCompletoCommand>
{
    private readonly IOrcamentoRepository _repo;
    private readonly IProcedimentoCirurgicoRepository _procedimentoRepo;
    private readonly IItemInventarioRepository _inventarioRepo;

    public AtualizarOrcamentoCompletoCommandHandler(
        IOrcamentoRepository repo,
        IProcedimentoCirurgicoRepository procedimentoRepo,
        IItemInventarioRepository inventarioRepo)
    {
        _repo = repo;
        _procedimentoRepo = procedimentoRepo;
        _inventarioRepo = inventarioRepo;
    }

    public async Task Handle(AtualizarOrcamentoCompletoCommand cmd)
    {
        var orcamento = await _repo.ObterPorIdCompleto(cmd.OrcamentoId);
        if (orcamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado neste estabelecimento.");

        var tipo = OrcamentoCompletoMapping.ParseTipoOrcamento(cmd.Tipo);

        if (cmd.ProcedimentoCirurgicoId is { } procId)
        {
            var proc = await _procedimentoRepo.ObterPorId(procId);
            if (proc.EstabelecimentoId != cmd.EstabelecimentoId || proc.PacienteId != orcamento.PacienteId)
                throw new BusinessException("Procedimento cirúrgico não pertence ao paciente neste estabelecimento.");
        }

        await ValidarImplantesCatalogo(cmd.Implantes, cmd.EstabelecimentoId);
        await ValidarCirurgiasCatalogo(cmd.Cirurgias, cmd.EstabelecimentoId, orcamento.PacienteId);

        orcamento.AtualizarCompleto(
            cmd.Validade,
            cmd.Observacoes,
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
