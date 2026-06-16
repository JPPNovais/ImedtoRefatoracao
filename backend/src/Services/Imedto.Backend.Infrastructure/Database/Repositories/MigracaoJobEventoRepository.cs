using Imedto.Backend.Domain.Migracao;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public sealed class MigracaoJobEventoRepository : IMigracaoJobEventoRepository
{
    private readonly AppDbContext _db;

    public MigracaoJobEventoRepository(AppDbContext db) => _db = db;

    public async Task Gravar(MigracaoJobEvento evento, CancellationToken ct = default)
    {
        _db.MigracaoJobEventos.Add(evento);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MigracaoJobEvento>> ListarPorJob(long jobId, CancellationToken ct = default)
        => await _db.MigracaoJobEventos
            .Where(e => e.MigracaoJobId == jobId)
            .OrderBy(e => e.CriadoEm)
            .ToListAsync(ct);
}
