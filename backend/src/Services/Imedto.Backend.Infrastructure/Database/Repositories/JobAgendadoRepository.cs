using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Jobs;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório EF de <see cref="JobAgendado"/>. Tabela pequena, leitura também via EF.
/// </summary>
public class JobAgendadoRepository : IJobAgendadoRepository
{
    private readonly AppDbContext _db;

    public JobAgendadoRepository(AppDbContext db) => _db = db;

    public async Task<List<JobAgendado>> ListarProntosParaExecutar(DateTime agora)
    {
        return await _db.JobsAgendados
            .Where(j => j.Status == JobStatus.Pendente && j.ProximoRunEm <= agora)
            .OrderBy(j => j.ProximoRunEm)
            .ToListAsync();
    }

    public async Task<JobAgendado?> ObterPorNomeOuNulo(string nome)
    {
        return await _db.JobsAgendados.FirstOrDefaultAsync(j => j.Nome == nome);
    }

    public async Task Salvar(JobAgendado job)
    {
        if (job.Id == 0)
            _db.JobsAgendados.Add(job);
        else
            _db.JobsAgendados.Update(job);

        await _db.SaveChangesAsync();
    }
}
