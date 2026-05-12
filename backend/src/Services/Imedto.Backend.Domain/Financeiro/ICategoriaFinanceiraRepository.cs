namespace Imedto.Backend.Domain.Financeiro;

public interface ICategoriaFinanceiraRepository
{
    /// <summary>
    /// Carrega a categoria filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<CategoriaFinanceira?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Indica se já existe categoria com mesmo nome+tipo no estabelecimento.
    /// Usado pelo handler para retornar 422 limpo antes do INSERT — sem isso a
    /// unique constraint do DB lança DbUpdateException que vira 500 genérico.
    /// </summary>
    Task<bool> ExisteComNomeETipo(string nome, string tipo, long estabelecimentoId);

    Task Salvar(CategoriaFinanceira categoria);
}
