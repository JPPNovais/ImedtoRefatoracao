using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório EF para <see cref="MigracaoJob"/>.
/// Falha-fechada: toda query filtra por estabelecimento_id — ausência de tenant lança.
/// </summary>
public class MigracaoJobRepository : IMigracaoJobRepository
{
    private readonly AppDbContext _db;

    public MigracaoJobRepository(AppDbContext db) => _db = db;

    public async Task Salvar(MigracaoJob job, CancellationToken ct = default)
    {
        if (job.Id == 0)
            _db.MigracaoJobs.Add(job);
        else
            _db.MigracaoJobs.Update(job);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MigracaoJob?> ObterPorIdDoEstabelecimentoOuNulo(
        long jobId,
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        // Falha-fechada: estabelecimento_id sempre no WHERE (CA2).
        if (estabelecimentoId <= 0)
            throw new InvalidOperationException("Tenant não definido — query bloqueada.");

        return await _db.MigracaoJobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.EstabelecimentoId == estabelecimentoId, ct);
    }

    public async Task<List<MigracaoJob>> ListarComArquivoParaExpirar(DateTime corte, CancellationToken ct = default)
    {
        return await _db.MigracaoJobs
            .Where(j => !j.ArquivoExpirado
                        && j.ArquivoExpiraEm != null
                        && j.ArquivoExpiraEm <= corte
                        && j.ArquivoS3Key != null)
            .ToListAsync(ct);
    }
}
