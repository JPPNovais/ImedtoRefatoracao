namespace Imedto.Backend.Domain.Pacientes;

/// <summary>
/// Servico de auditoria de acesso a paciente (LGPD).
/// Implementacao append-only persistida em <c>paciente_acesso_log</c>.
///
/// Falhas de gravacao do log NAO devem quebrar o fluxo do usuario —
/// implementacao engole excecao + LogError. O servico eh "best-effort"
/// em termos de UX, mas chamado obrigatoriamente em todo acesso a PII.
/// </summary>
public interface IPacienteAcessoLogService
{
    Task RegistrarAsync(
        long pacienteId,
        Guid usuarioId,
        long estabelecimentoId,
        TipoAcessoPaciente tipoAcesso,
        string? ipOrigem = null);
}
