using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório EF para <see cref="MigracaoMapa"/>.
/// Falha-fechada: toda query filtra por estabelecimentoId (CA2, multi-tenant).
/// </summary>
public class MigracaoMapaRepository : IMigracaoMapaRepository
{
    private readonly AppDbContext _db;

    public MigracaoMapaRepository(AppDbContext db) => _db = db;

    public async Task Salvar(MigracaoMapa mapa, CancellationToken ct = default)
    {
        if (mapa.Id == 0)
            _db.MigracaoMapas.Add(mapa);
        else
            _db.MigracaoMapas.Update(mapa);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MigracaoMapa>> ListarPorJob(
        long jobId,
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0)
            throw new InvalidOperationException("Tenant não definido — query bloqueada.");

        return await _db.MigracaoMapas
            .Where(m => m.MigracaoJobId == jobId && m.EstabelecimentoId == estabelecimentoId)
            .OrderBy(m => m.Entidade)
            .ToListAsync(ct);
    }

    public async Task<MigracaoMapa?> ObterPorJobEEntidadeOuNulo(
        long jobId,
        string entidade,
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0)
            throw new InvalidOperationException("Tenant não definido — query bloqueada.");

        return await _db.MigracaoMapas
            .FirstOrDefaultAsync(m =>
                m.MigracaoJobId == jobId &&
                m.Entidade == entidade &&
                m.EstabelecimentoId == estabelecimentoId, ct);
    }

    public async Task<MigracaoMapa?> ObterPorJobEEntidadeAdminOuNulo(
        long jobId,
        string entidade,
        CancellationToken ct = default)
    {
        return await _db.MigracaoMapas
            .FirstOrDefaultAsync(m => m.MigracaoJobId == jobId && m.Entidade == entidade, ct);
    }
}
