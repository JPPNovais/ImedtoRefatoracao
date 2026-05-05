namespace Imedto.Backend.Domain.Financeiro;

public interface ICategoriaFinanceiraRepository
{
    /// <summary>
    /// Carrega a categoria filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<CategoriaFinanceira?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(CategoriaFinanceira categoria);
}
