using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ItemInventarioRepository : IItemInventarioRepository
{
    private readonly AppDbContext _db;

    public ItemInventarioRepository(AppDbContext db) => _db = db;

    public async Task<ItemInventario> ObterPorId(long id)
    {
        var item = await ObterPorIdOuNulo(id);
        if (item is null)
            throw new BusinessException("Item de inventário não encontrado.");
        return item;
    }

    public async Task<ItemInventario?> ObterPorIdOuNulo(long id)
        => await _db.ItensInventario.FindAsync(id);

    public async Task Salvar(ItemInventario item)
    {
        if (item.Id == 0)
            _db.ItensInventario.Add(item);
        else
            _db.ItensInventario.Update(item);
        await _db.SaveChangesAsync();
    }
}
