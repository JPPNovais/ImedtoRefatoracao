using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Pacientes;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Implementacao do audit log de paciente. Best-effort: erros de gravacao
/// nao quebram o fluxo do usuario (engole excecao + LogError).
///
/// SaveChangesAsync direto aqui (igual ao ProntuarioAcessoLogService) —
/// audit deve ser commitado mesmo se a transacao do UoW depois falhar.
/// </summary>
public class PacienteAcessoLogService : IPacienteAcessoLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PacienteAcessoLogService> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public PacienteAcessoLogService(
        AppDbContext context,
        ILogger<PacienteAcessoLogService> logger,
        IHttpContextAccessor httpContext)
    {
        _context = context;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task RegistrarAsync(
        long pacienteId,
        Guid usuarioId,
        long estabelecimentoId,
        TipoAcessoPaciente tipoAcesso,
        string? ipOrigem = null)
    {
        try
        {
            // Resolve IP automaticamente quando o caller não passa — caso normal vindo
            // de controller HTTP. Honra X-Forwarded-For (Caddy/proxy reverso). Coluna
            // ip_origem em paciente_acesso_log é varchar(45), aceita IPv4 e IPv6.
            var ip = ipOrigem ?? ResolverIpOrigem();
            var log = PacienteAcessoLog.Registrar(pacienteId, usuarioId, estabelecimentoId, tipoAcesso, ip);
            await _context.PacienteAcessoLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Nao deixar audit falho quebrar o fluxo do usuario. Log estruturado
            // (sem PII) permite alertar SRE e investigar separadamente.
            _logger.LogError(ex,
                "Falha ao gravar PacienteAcessoLog. Paciente={PacienteId}, Usuario={UsuarioId}, " +
                "Estabelecimento={EstabelecimentoId}, Tipo={TipoAcesso}",
                pacienteId, usuarioId, estabelecimentoId, tipoAcesso);
        }
    }

    private string? ResolverIpOrigem()
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null) return null;

        // X-Forwarded-For tem prioridade quando há proxy reverso (Caddy).
        // Formato: "client, proxy1, proxy2..." — o primeiro IP é o origem real.
        var fwd = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            var primeiro = fwd.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(primeiro)) return Truncar(primeiro);
        }

        return Truncar(ctx.Connection.RemoteIpAddress?.ToString());
    }

    // Coluna varchar(45) cobre IPv4 (15) e IPv6 (39); guard contra header malicioso.
    private static string? Truncar(string? ip) =>
        ip is { Length: > 45 } ? ip[..45] : ip;
}
