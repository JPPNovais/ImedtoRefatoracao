using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>Repositório de escrita (EF Core) para modelos de prontuário globais.</summary>
public class ImedtoModeloProntuarioGlobalRepository
{
    private readonly AppDbContext _db;

    public ImedtoModeloProntuarioGlobalRepository(AppDbContext db) => _db = db;

    public async Task<ImedtoModeloProntuarioGlobal?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ImedtoModelosProntuarioGlobal.FindAsync([id], ct);

    public void Adicionar(ImedtoModeloProntuarioGlobal modelo) => _db.ImedtoModelosProntuarioGlobal.Add(modelo);

    public Task SalvarAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
