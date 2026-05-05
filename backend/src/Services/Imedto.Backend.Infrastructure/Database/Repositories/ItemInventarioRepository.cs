using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Inventario;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ItemInventarioRepository : IItemInventarioRepository
{
    private readonly AppDbContext _db;

    public ItemInventarioRepository(AppDbContext db) => _db = db;

    public async Task<ItemInventario?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.ItensInventario
            .FirstOrDefaultAsync(i => i.Id == id && i.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(ItemInventario item)
    {
        if (item.Id == 0)
            _db.ItensInventario.Add(item);
        else
            _db.ItensInventario.Update(item);
        await _db.SaveChangesAsync();
    }
}
