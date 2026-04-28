namespace Imedto.Backend.Domain.Inventario;

public interface IItemInventarioRepository
{
    Task<ItemInventario> ObterPorId(long id);
    Task<ItemInventario?> ObterPorIdOuNulo(long id);
    Task Salvar(ItemInventario item);
}
