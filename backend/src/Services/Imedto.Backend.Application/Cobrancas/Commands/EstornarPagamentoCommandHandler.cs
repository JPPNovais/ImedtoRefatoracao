using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cobrancas.Commands;

/// <summary>
/// Estorna um pagamento de cobrança (INV-7).
/// Atômico: EstornoPagamento + Lancamento negativo na mesma transação UoW.
/// Pagamento original permanece imutável. Status recalculado por soma líquida.
/// [UnitOfWork] no endpoint garante o rollback se qualquer SaveChanges falhar.
/// </summary>
public class EstornarPagamentoCommandHandler : ICommandHandler<EstornarPagamentoCommand>
{
    private readonly ICobrancaRepository _cobrancaRepo;
    private readonly IEstornoPagamentoRepository _estornoRepo;
    private readonly ILancamentoRepository _lancamentoRepo;

    public EstornarPagamentoCommandHandler(
        ICobrancaRepository cobrancaRepo,
        IEstornoPagamentoRepository estornoRepo,
        ILancamentoRepository lancamentoRepo)
    {
        _cobrancaRepo = cobrancaRepo;
        _estornoRepo = estornoRepo;
        _lancamentoRepo = lancamentoRepo;
    }

    public async Task Handle(EstornarPagamentoCommand cmd)
    {
        // R11: falha-fechada por tenant
        var cobranca = await _cobrancaRepo.ObterPorIdOuNulo(cmd.CobrancaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Não encontrado.");

        // INV-7 / R4: gera EstornoPagamento no aggregate (valida R5/R8/R12 internamente)
        var estorno = cobranca.EstornarPagamento(
            pagamentoId: cmd.PagamentoId,
            motivo: cmd.Motivo,
            estornadoPorUsuarioId: cmd.UsuarioId);

        // Persiste o aggregate (novo EstornoPagamento rastreado pelo EF) para obter o Id.
        await _cobrancaRepo.Salvar(cobranca);

        // Cria Lancamento de estorno — valor negativo + categoria Estorno (DC2/INV-7).
        var lancamentoEstorno = Lancamento.CriarParaEstorno(
            estabelecimentoId: cmd.EstabelecimentoId,
            valorEstornado: estorno.Valor,
            dataEstorno: estorno.DataEstorno,
            criadoPorUsuarioId: cmd.UsuarioId,
            cobrancaId: cobranca.Id,
            pagamentoId: cmd.PagamentoId);

        await _lancamentoRepo.Salvar(lancamentoEstorno);

        // Fecha o anel EstornoPagamento → LancamentoEstorno (padrão INV-3 da F1 aplicado à F2).
        estorno.VincularLancamento(lancamentoEstorno.Id);

        // Persiste o LancamentoEstornoId no EstornoPagamento.
        await _cobrancaRepo.Salvar(cobranca);
    }
}
