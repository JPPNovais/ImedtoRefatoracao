using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> populando as categorias e formas de
/// pagamento padrão do estabelecimento (item 2.10 da Fase 2 — espelha o seed que existia no
/// legado). Tudo é criado via fábrica <c>CriarPadrao</c>, então não pode ser editado/inativado
/// pelos handlers comuns.
/// </summary>
public class CriarSeedFinanceiroAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly ICategoriaFinanceiraRepository _categoriaRepo;
    private readonly IFormaPagamentoRepository _formaPagamentoRepo;

    public CriarSeedFinanceiroAoCriarEstabelecimentoHandler(
        ICategoriaFinanceiraRepository categoriaRepo,
        IFormaPagamentoRepository formaPagamentoRepo)
    {
        _categoriaRepo = categoriaRepo;
        _formaPagamentoRepo = formaPagamentoRepo;
    }

    public async Task Handle(EstabelecimentoCriadoEvent domainEvent)
    {
        foreach (var (nome, tipo) in SeedsFinanceiro.Categorias)
        {
            var categoria = CategoriaFinanceira.CriarPadrao(domainEvent.EstabelecimentoId, nome, tipo);
            await _categoriaRepo.Salvar(categoria);
        }

        foreach (var nome in SeedsFinanceiro.FormasPagamento)
        {
            var forma = FormaPagamento.CriarPadrao(domainEvent.EstabelecimentoId, nome);
            await _formaPagamentoRepo.Salvar(forma);
        }
    }
}
