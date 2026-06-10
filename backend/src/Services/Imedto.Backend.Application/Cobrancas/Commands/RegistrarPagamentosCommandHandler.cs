using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cobrancas.Commands;

/// <summary>
/// Registra um ou mais pagamentos para uma cobrança (R11 — múltiplas formas na mesma transação).
/// Cada pagamento gera um Lancamento atomicamente (INV-3).
/// A atomicidade é garantida pelo [UnitOfWork] no controller — ambos SaveChanges no mesmo EF DbContext.
/// </summary>
public class RegistrarPagamentosCommandHandler : ICommandHandler<RegistrarPagamentosCommand>
{
    private readonly ICobrancaRepository _cobrancaRepo;
    private readonly ILancamentoRepository _lancamentoRepo;
    private readonly IConfigTaxaFormaPagamentoRepository _configTaxaRepo;

    public RegistrarPagamentosCommandHandler(
        ICobrancaRepository cobrancaRepo,
        ILancamentoRepository lancamentoRepo,
        IConfigTaxaFormaPagamentoRepository configTaxaRepo)
    {
        _cobrancaRepo = cobrancaRepo;
        _lancamentoRepo = lancamentoRepo;
        _configTaxaRepo = configTaxaRepo;
    }

    public async Task Handle(RegistrarPagamentosCommand cmd)
    {
        if (!cmd.Formas.Any())
            throw new BusinessException("Informe ao menos uma forma de pagamento.");

        // R14: filtro por tenant (falha-fechada)
        var cobranca = await _cobrancaRepo.ObterPorIdOuNulo(cmd.CobrancaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Não encontrado.");

        // Aplica desconto se informado (INV-4/INV-8)
        if (cmd.Desconto > 0)
            cobranca.AplicarDesconto(cmd.Desconto, cmd.PodeAplicarDesconto);

        // INV-3: registra pagamentos + lançamentos na mesma transação UoW do controller.
        // Ordem de persistência por iteração:
        //  1. cobranca.RegistrarPagamento → adiciona Pagamento à coleção (EF rastreia)
        //  2. _cobrancaRepo.Salvar        → SaveChanges → Pagamento.Id gerado pelo banco
        //  3. Cria Lancamento com CobrancaId + PagamentoId já conhecidos
        //  4. _lancamentoRepo.Salvar      → SaveChanges → Lancamento.Id gerado
        //  5. pagamento.VincularLancamento → atualiza Pagamento.LancamentoId no mesmo contexto
        //  6. _cobrancaRepo.Salvar        → SaveChanges → persiste LancamentoId no Pagamento
        // Todo o ciclo ocorre dentro da mesma transação UoW aberta pelo UnitOfWorkFilter global.
        foreach (var forma in cmd.Formas)
        {
            // R10: busca taxa da config; null = 0%
            var configTaxa = await _configTaxaRepo.ObterPorForma(cmd.EstabelecimentoId, forma.FormaPagamentoId);
            var taxa = configTaxa?.CalcularTaxa(forma.Valor) ?? 0m;

            var pagamento = cobranca.RegistrarPagamento(
                forma.Valor,
                forma.FormaPagamentoId,
                forma.Parcelas,
                forma.Juros,
                taxa,
                cmd.DataPagamento,
                cmd.UsuarioId);

            // Persiste o Pagamento para obter o Id gerado pelo banco.
            await _cobrancaRepo.Salvar(cobranca);

            // Cria Lancamento com PagamentoId já disponível (INV-3 bidirecional).
            var lancamento = Lancamento.CriarParaPagamento(
                estabelecimentoId: cmd.EstabelecimentoId,
                descricao: "Pagamento de consulta",
                valor: forma.Valor,
                dataVencimento: cmd.DataPagamento,
                categoria: "Receita: Consulta",
                criadoPorUsuarioId: cmd.UsuarioId,
                cobrancaId: cobranca.Id);

            lancamento.VincularPagamento(pagamento.Id);

            // Marca como Pago imediatamente (pagamento já ocorreu — não é conta a receber).
            lancamento.Pagar(cmd.DataPagamento);

            await _lancamentoRepo.Salvar(lancamento);

            // INV-3: fecha o anel Pagamento → Lancamento.
            pagamento.VincularLancamento(lancamento.Id);
        }

        // Último SaveChanges para persistir VincularLancamento em todos os pagamentos.
        await _cobrancaRepo.Salvar(cobranca);
    }
}
