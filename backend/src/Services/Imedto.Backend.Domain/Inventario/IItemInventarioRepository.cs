namespace Imedto.Backend.Domain.Inventario;

public interface IItemInventarioRepository
{
    /// <summary>
    /// Carrega o item de inventário filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<ItemInventario?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Indica se já existe item com o mesmo código no estabelecimento.
    /// Usado pelo handler de Criar para retornar 422 antes do INSERT — sem isso,
    /// a unique constraint do DB lança DbUpdateException que vira 500 genérico.
    /// </summary>
    Task<bool> ExisteComCodigoNoEstabelecimento(string codigo, long estabelecimentoId);

    Task Salvar(ItemInventario item);

    Task<ItemInventario?> ObterPorCodigoOuNulo(string codigo, long estabelecimentoId);
    Task<ItemInventario?> ObterPorNomeOuNulo(string nome, long estabelecimentoId);
}
