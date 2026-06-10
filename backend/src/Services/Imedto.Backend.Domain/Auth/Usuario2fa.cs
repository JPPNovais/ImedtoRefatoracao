using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Estado de 2FA (TOTP) de um usuário. Relação 1:1 opcional com <c>usuarios.id</c>.
/// Separado de <see cref="AuthCredencial"/> para manter a credencial focada em senha/lockout.
///
/// O segredo TOTP <b>nunca</b> é armazenado em claro — <see cref="SegredoCifrado"/> contém
/// o output de <c>IDataProtector.Protect</c> (purpose <c>"auth.totp.secret"</c>).
/// </summary>
public class Usuario2fa : Entity<Guid>
{
    /// <summary>PK = ID do usuário (relação 1:1). Não é gerado automaticamente.</summary>
    public virtual Guid UsuarioId { get; protected set; }

    /// <summary>
    /// Segredo TOTP cifrado com IDataProtectionProvider purpose "auth.totp.secret".
    /// Nunca retornar em payload após a ativação.
    /// </summary>
    public virtual string SegredoCifrado { get; protected set; }

    /// <summary>Ciclo de vida do 2FA: Pendente (gerado, aguardando confirmação) → Ativo.</summary>
    public virtual Usuario2faStatus Status { get; protected set; }

    /// <summary>Quando o usuário confirmou o TOTP e o 2FA ficou ativo.</summary>
    public virtual DateTime? AtivadoEm { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    public virtual bool Ativo => Status == Usuario2faStatus.Ativo;

    protected Usuario2fa() { }

    /// <summary>
    /// Cria o estado inicial Pendente. O segredo ainda não está confirmado pelo usuário.
    /// </summary>
    public static Usuario2fa IniciarAtivacao(Guid usuarioId, string segredoCifrado)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("UsuarioId é obrigatório.");
        if (string.IsNullOrWhiteSpace(segredoCifrado))
            throw new BusinessException("Segredo cifrado é obrigatório.");

        return new Usuario2fa
        {
            Id = usuarioId,
            UsuarioId = usuarioId,
            SegredoCifrado = segredoCifrado,
            Status = Usuario2faStatus.Pendente,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Confirma a ativação após validação do código TOTP. Transição Pendente → Ativo.
    /// </summary>
    public virtual void ConfirmarAtivacao()
    {
        if (Status == Usuario2faStatus.Ativo)
            throw new BusinessException("2FA já está ativo.");

        Status = Usuario2faStatus.Ativo;
        AtivadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza o segredo (ex.: reativação após desativar). Mantém o status atual.
    /// </summary>
    public virtual void AtualizarSegredo(string segredoCifrado)
    {
        if (string.IsNullOrWhiteSpace(segredoCifrado))
            throw new BusinessException("Segredo cifrado é obrigatório.");

        SegredoCifrado = segredoCifrado;
        Status = Usuario2faStatus.Pendente;
        AtivadoEm = null;
        AtualizadoEm = DateTime.UtcNow;
    }
}

/// <summary>Ciclo de vida do 2FA do usuário.</summary>
public enum Usuario2faStatus
{
    /// <summary>Segredo gerado, aguardando confirmação com código TOTP válido.</summary>
    Pendente,

    /// <summary>Código TOTP confirmado; 2FA em pleno vigor no login.</summary>
    Ativo
}
