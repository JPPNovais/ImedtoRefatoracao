using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Log de auditoria append-only para ações de segurança de conta do usuário.
/// Armazena ativação/desativação de 2FA e uso de código de recuperação.
///
/// Logins bem-sucedidos com TOTP NÃO geram linha aqui (são login normal).
/// Sem PII: sem e-mail, CPF, telefone nem segredo TOTP.
/// Retenção sugerida: 365 dias (alinhado a <c>paciente_acesso_log</c>).
/// </summary>
public class UsuarioSegurancaAudit : Entity<long>
{
    public virtual Guid UsuarioId { get; protected set; }

    /// <summary>Ação auditada. Valores: <see cref="AcaoSeguranca"/>.</summary>
    public virtual string Acao { get; protected set; }

    public virtual DateTime OcorridoEm { get; protected set; }

    /// <summary>IP de origem do request (IPv4/IPv6, máx. 45 chars). Opcional.</summary>
    public virtual string IpOrigem { get; protected set; }

    protected UsuarioSegurancaAudit() { }

    public static UsuarioSegurancaAudit Registrar(Guid usuarioId, string acao, string ipOrigem = null)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("UsuarioId é obrigatório.");
        if (string.IsNullOrWhiteSpace(acao))
            throw new BusinessException("Ação é obrigatória.");

        return new UsuarioSegurancaAudit
        {
            UsuarioId = usuarioId,
            Acao = acao.Trim(),
            OcorridoEm = DateTime.UtcNow,
            IpOrigem = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim()
        };
    }
}

/// <summary>Ações auditáveis de segurança de conta (2FA).</summary>
public static class AcaoSeguranca
{
    public const string Ativou2fa = "Ativou2fa";
    public const string Desativou2fa = "Desativou2fa";
    public const string UsouCodigoRecuperacao = "UsouCodigoRecuperacao";
}
