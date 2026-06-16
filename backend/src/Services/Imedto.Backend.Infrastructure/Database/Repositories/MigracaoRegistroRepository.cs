using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class MigracaoRegistroRepository : IMigracaoRegistroRepository
{
    private readonly AppDbContext _db;
    public MigracaoRegistroRepository(AppDbContext db) => _db = db;

    public async Task SalvarLote(IReadOnlyList<MigracaoRegistro> registros, CancellationToken ct = default)
    {
        _db.MigracaoRegistros.AddRange(registros);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Salvar(MigracaoRegistro registro, CancellationToken ct = default)
    {
        if (registro.Id == 0)
            _db.MigracaoRegistros.Add(registro);
        else
            _db.MigracaoRegistros.Update(registro);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MigracaoRegistro>> ListarPorJob(long jobId, CancellationToken ct = default)
        => await _db.MigracaoRegistros
            .Where(r => r.MigracaoJobId == jobId)
            .ToListAsync(ct);

    public async Task<List<MigracaoRegistro>> ListarCriadosPorJob(long jobId, CancellationToken ct = default)
        => await _db.MigracaoRegistros
            .Where(r => r.MigracaoJobId == jobId && r.Status == "importado_criado")
            .ToListAsync(ct);

    public async Task DeletarPendentesPorJob(long jobId, CancellationToken ct = default)
    {
        // Addendum 6 — R-M7/CA110: DELETE coberto pelo índice (migracao_job_id, status).
        // Nunca toca importado_criado/importado_atualizado/rejeitado/pulado.
        await _db.MigracaoRegistros
            .Where(r => r.MigracaoJobId == jobId && r.Status == "pendente")
            .ExecuteDeleteAsync(ct);
    }

    public async Task<RelatorioMigracao> ObterRelatorio(long jobId, CancellationToken ct = default)
    {
        var registros = await _db.MigracaoRegistros
            .Where(r => r.MigracaoJobId == jobId)
            .ToListAsync(ct);

        var relatorio = new RelatorioMigracao
        {
            TotalCriados = registros.Count(r => r.Status == "importado_criado"),
            TotalAtualizados = registros.Count(r => r.Status == "importado_atualizado"),
            TotalRejeitados = registros.Count(r => r.Status == "rejeitado"),
            TotalPulados = registros.Count(r => r.Status == "pulado"),
        };

        // Addendum 002 — D-C1/D-C2/CA34/CA35: agrega motivo → quantidade por entidade.
        // MotivoRejeicao já é categoria genérica sem PII por design (R3/CA4 do briefing original).
        relatorio.PorEntidade = registros
            .GroupBy(r => r.Entidade)
            .ToDictionary(
                g => g.Key,
                g => new RelatorioEntidade
                {
                    Criados = g.Count(r => r.Status == "importado_criado"),
                    Atualizados = g.Count(r => r.Status == "importado_atualizado"),
                    Rejeitados = g.Count(r => r.Status == "rejeitado"),
                    Pulados = g.Count(r => r.Status == "pulado"),
                    MotivosRejeicao = g
                        .Where(r => r.Status == "rejeitado" && r.MotivoRejeicao != null)
                        .GroupBy(r => r.MotivoRejeicao!)
                        .ToDictionary(mg => mg.Key, mg => mg.Count()),
                    MotivosPulo = g
                        .Where(r => r.Status == "pulado" && r.MotivoRejeicao != null)
                        .GroupBy(r => r.MotivoRejeicao!)
                        .ToDictionary(mg => mg.Key, mg => mg.Count()),
                });

        return relatorio;
    }
}
