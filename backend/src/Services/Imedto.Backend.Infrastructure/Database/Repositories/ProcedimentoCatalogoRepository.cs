using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Catalogo;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProcedimentoCatalogoRepository : IProcedimentoCatalogoRepository
{
    private readonly AppDbContext _db;

    public ProcedimentoCatalogoRepository(AppDbContext db) => _db = db;

    public async Task<ProcedimentoCatalogo?> ObterPorIdOuNulo(long id) =>
        await _db.CatalogoProcedimentos.FirstOrDefaultAsync(p => p.Id == id);

    public async Task Salvar(ProcedimentoCatalogo procedimento)
    {
        if (procedimento.Id == 0)
        {
            await _db.CatalogoProcedimentos.AddAsync(procedimento);
            await _db.SaveChangesAsync();
        }
    }
}
