namespace Imedto.Backend.Domain.Inventario;

public interface IItemInventarioRepository
{
    /// <summary>
    /// Carrega o item de inventário filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<ItemInventario?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(ItemInventario item);
}
