using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> populando as categorias e formas de
/// pagamento padrão do estabelecimento.
///
/// Categorias: lidas do catálogo global <see cref="ICategoriaFinanceiraPadraoSistemaRepository"/>
/// (R2 — briefing 2026-06-22_003 M2). Apenas as ativas são copiadas.
/// FormasPagamento: continua hardcoded em <see cref="SeedsFinanceiro.FormasPagamento"/> (CA22).
/// </summary>
public class CriarSeedFinanceiroAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly ICategoriaFinanceiraPadraoSistemaRepository _padraoRepo;
    private readonly ICategoriaFinanceiraRepository _categoriaRepo;
    private readonly IFormaPagamentoRepository _formaPagamentoRepo;

    public CriarSeedFinanceiroAoCriarEstabelecimentoHandler(
        ICategoriaFinanceiraPadraoSistemaRepository padraoRepo,
        ICategoriaFinanceiraRepository categoriaRepo,
        IFormaPagamentoRepository formaPagamentoRepo)
    {
        _padraoRepo = padraoRepo;
        _categoriaRepo = categoriaRepo;
        _formaPagamentoRepo = formaPagamentoRepo;
    }

    public async Task Handle(EstabelecimentoCriadoEvent domainEvent)
    {
        // R2 — seed lê do catálogo global (sem hardcode de SeedsFinanceiro.Categorias).
        var categoriasGlobais = await _padraoRepo.ListarAtivas();
        foreach (var global in categoriasGlobais)
        {
            var categoria = CategoriaFinanceira.CriarPadrao(domainEvent.EstabelecimentoId, global.Nome, global.Tipo);
            await _categoriaRepo.Salvar(categoria);
        }

        // CA22 — FormasPagamento permanece hardcoded (intocado).
        foreach (var nome in SeedsFinanceiro.FormasPagamento)
        {
            var forma = FormaPagamento.CriarPadrao(domainEvent.EstabelecimentoId, nome);
            await _formaPagamentoRepo.Salvar(forma);
        }
    }
}
