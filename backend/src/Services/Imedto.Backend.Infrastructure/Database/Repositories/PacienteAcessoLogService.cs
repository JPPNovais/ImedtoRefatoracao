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

    public PacienteAcessoLogService(AppDbContext context, ILogger<PacienteAcessoLogService> logger)
    {
        _context = context;
        _logger = logger;
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
            var log = PacienteAcessoLog.Registrar(pacienteId, usuarioId, estabelecimentoId, tipoAcesso, ipOrigem);
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
}
