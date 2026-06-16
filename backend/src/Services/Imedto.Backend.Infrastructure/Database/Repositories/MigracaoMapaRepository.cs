using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório EF para <see cref="MigracaoMapa"/>.
/// Falha-fechada: toda query filtra por estabelecimentoId (CA2, multi-tenant).
/// Addendum 4: chave de upsert é (jobId, entidade, nomeBlocoOrigem).
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
            .ThenBy(m => m.NomeBlocoOrigem)
            .ToListAsync(ct);
    }

    public async Task<MigracaoMapa?> ObterPorJobEntidadeBlocoOuNulo(
        long jobId,
        string entidade,
        string nomeBlocoOrigem,
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0)
            throw new InvalidOperationException("Tenant não definido — query bloqueada.");

        return await _db.MigracaoMapas
            .FirstOrDefaultAsync(m =>
                m.MigracaoJobId == jobId &&
                m.Entidade == entidade &&
                m.NomeBlocoOrigem == nomeBlocoOrigem &&
                m.EstabelecimentoId == estabelecimentoId, ct);
    }

    public async Task<MigracaoMapa?> ObterPorJobEEntidadeOuNulo(
        long jobId,
        string entidade,
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        // Compatibilidade: usa nomeBlocoOrigem = "" (CSV/JSON-array existente).
        return await ObterPorJobEntidadeBlocoOuNulo(jobId, entidade, string.Empty, estabelecimentoId, ct);
    }

    public async Task<MigracaoMapa?> ObterPorJobEEntidadeAdminOuNulo(
        long jobId,
        string entidade,
        CancellationToken ct = default)
    {
        return await _db.MigracaoMapas
            .FirstOrDefaultAsync(m => m.MigracaoJobId == jobId && m.Entidade == entidade, ct);
    }

    /// <summary>
    /// Addendum 5 — reprocessar parcial (CA97/R-R8):
    /// Busca mapa por (jobId, nomeBlocoOrigem) sem filtrar por entidade.
    /// </summary>
    public async Task<MigracaoMapa?> ObterPorJobBlocoOuNulo(
        long jobId,
        string nomeBlocoOrigem,
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        if (estabelecimentoId <= 0)
            throw new InvalidOperationException("Tenant não definido — query bloqueada.");

        return await _db.MigracaoMapas
            .FirstOrDefaultAsync(m =>
                m.MigracaoJobId == jobId &&
                m.NomeBlocoOrigem == nomeBlocoOrigem &&
                m.EstabelecimentoId == estabelecimentoId, ct);
    }

    /// <summary>
    /// Addendum 4: busca por bloco sem filtro de tenant — uso exclusivo do admin.
    /// Para CSV/JSON-array (nomeBlocoOrigem = ""), cai no método legado por compatibilidade.
    /// </summary>
    public async Task<MigracaoMapa?> ObterPorJobEntidadeBlocoAdminOuNulo(
        long jobId,
        string entidade,
        string nomeBlocoOrigem,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(nomeBlocoOrigem))
            return await ObterPorJobEEntidadeAdminOuNulo(jobId, entidade, ct);

        return await _db.MigracaoMapas
            .FirstOrDefaultAsync(m =>
                m.MigracaoJobId == jobId &&
                m.Entidade == entidade &&
                m.NomeBlocoOrigem == nomeBlocoOrigem, ct);
    }
}
