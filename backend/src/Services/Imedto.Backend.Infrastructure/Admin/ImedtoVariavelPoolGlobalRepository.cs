using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>Repositório de escrita (EF Core) para variáveis pool globais.</summary>
public class ImedtoVariavelPoolGlobalRepository
{
    private readonly AppDbContext _db;

    public ImedtoVariavelPoolGlobalRepository(AppDbContext db) => _db = db;

    public async Task<ImedtoVariavelPoolGlobal?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ImedtoVariaveisPoolGlobal.FindAsync([id], ct);

    public void Adicionar(ImedtoVariavelPoolGlobal variavel) => _db.ImedtoVariaveisPoolGlobal.Add(variavel);

    public Task SalvarAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
