using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>Repositório de escrita (EF Core) para regiões anatômicas globais.</summary>
public class ImedtoRegiaoAnatomicaGlobalRepository
{
    private readonly AppDbContext _db;

    public ImedtoRegiaoAnatomicaGlobalRepository(AppDbContext db) => _db = db;

    public async Task<ImedtoRegiaoAnatomicaGlobal?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ImedtoRegioesAnatomicasGlobal.FindAsync([id], ct);

    public void Adicionar(ImedtoRegiaoAnatomicaGlobal regiao) => _db.ImedtoRegioesAnatomicasGlobal.Add(regiao);

    public Task SalvarAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
