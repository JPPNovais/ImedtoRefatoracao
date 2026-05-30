using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Caminho único para registrar audit de ações do admin global.
///
/// LGPD: captura IP e UserAgent automaticamente via IHttpContextAccessor.
/// Nenhum campo de PII de paciente neste writer — apenas IDs e metadados.
///
/// Regra: falha em escrever audit em mutação = falha da operação (lança exceção).
///        Falha em audit de leitura = log de erro, não bloqueia (chamador decide).
/// </summary>
public class ImedtoAdminAuditWriter
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<ImedtoAdminAuditWriter> _logger;

    public ImedtoAdminAuditWriter(
        AppDbContext db,
        IHttpContextAccessor http,
        ILogger<ImedtoAdminAuditWriter> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Registra uma linha de audit. Salva imediatamente (SaveChangesAsync) — não
    /// depende de UnitOfWork externo para que o audit não seja perdido se o UoW
    /// da operação principal falhar depois.
    ///
    /// Para mutações: lançar exceção se falhar (caller não deve continuar).
    /// Para leituras: usar <see cref="RegistrarLeituraAsync"/> que silencia falhas.
    /// </summary>
    public virtual async Task RegistrarAsync(
        string acao,
        Guid? adminId,
        string? recursoTipo = null,
        string? recursoId = null,
        long? tenantAfetadoId = null,
        string? motivo = null,
        string? payloadJson = null,
        CancellationToken ct = default)
    {
        var ip = ObterIp();
        var ua = ObterUserAgent();

        var log = ImedtoAdminAuditLog.Registrar(
            acao,
            adminId,
            recursoTipo,
            recursoId,
            tenantAfetadoId,
            motivo,
            ip,
            ua,
            payloadJson);

        _db.ImedtoAdminAuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Registra audit de leitura de detalhe. Silencia erros — falha de audit de
    /// leitura não deve bloquear a operação de leitura.
    /// </summary>
    public virtual async Task RegistrarLeituraAsync(
        string acao,
        Guid adminId,
        string recursoTipo,
        string recursoId,
        long? tenantAfetadoId = null,
        string? motivo = null,
        CancellationToken ct = default)
    {
        try
        {
            await RegistrarAsync(acao, adminId, recursoTipo, recursoId, tenantAfetadoId, motivo, null, ct);
        }
        catch (Exception ex)
        {
            // Falha de audit de leitura é loggada mas não bloqueia. Apenas IDs — sem PII.
            _logger.LogError(ex,
                "Falha ao registrar audit de leitura. Acao={Acao} AdminId={AdminId} RecursoId={RecursoId}",
                acao, adminId, recursoId);
        }
    }

    private string? ObterIp()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;

        // X-Forwarded-For tem prioridade (proxy reverso em prod).
        var fwd = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
            return fwd.Split(',')[0].Trim();

        return ctx.Connection.RemoteIpAddress?.ToString();
    }

    private string? ObterUserAgent()
        => _http.HttpContext?.Request.Headers.UserAgent.ToString();
}
