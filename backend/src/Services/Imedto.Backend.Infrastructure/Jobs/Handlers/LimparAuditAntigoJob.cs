using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Jobs.Handlers;

/// <summary>
/// Limpa registros de tentativas de delete bloqueadas pelo <c>SoftDeleteInterceptor</c>
/// com mais de 90 dias. Mantemos a tabela enxuta sem perder a janela de auditoria
/// relevante para LGPD/forense recente.
/// </summary>
public class LimparAuditAntigoJob : IJobHandler
{
    public string Nome => "limpar-audit-antigo";

    private static readonly TimeSpan Retencao = TimeSpan.FromDays(90);

    private readonly AppDbContext _db;
    private readonly ILogger<LimparAuditAntigoJob> _logger;

    public LimparAuditAntigoJob(AppDbContext db, ILogger<LimparAuditAntigoJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var corte = DateTime.UtcNow - Retencao;

        var apagados = await _db.AuditDeleteAttempts
            .Where(a => a.TentadoEm < corte)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "[Job:{Nome}] Removidos {Apagados} registros de audit_delete_attempts anteriores a {Corte:o}.",
            Nome, apagados, corte);
    }
}
