using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Refresh token persistido com SHA-256 do token cru (nunca o token em si).
/// Cada refresh emitido cria uma linha aqui; o uso (rotação) revoga e emite novo.
/// </summary>
public class AuthRefreshToken : Entity<long>
{
    public virtual Guid UsuarioId { get; protected set; }
    public virtual string TokenHash { get; protected set; }
    public virtual DateTime ExpiraEm { get; protected set; }
    public virtual DateTime? RevogadoEm { get; protected set; }
    public virtual string IpOrigem { get; protected set; }
    public virtual string UserAgent { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected AuthRefreshToken() { }

    public static AuthRefreshToken Emitir(
        Guid usuarioId,
        string tokenHash,
        DateTime expiraEm,
        string ipOrigem,
        string userAgent)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("UsuarioId é obrigatório.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new BusinessException("Hash do token é obrigatório.");
        if (expiraEm <= DateTime.UtcNow)
            throw new BusinessException("Expiração deve ser no futuro.");

        return new AuthRefreshToken
        {
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            ExpiraEm = expiraEm,
            IpOrigem = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual bool Expirado => ExpiraEm <= DateTime.UtcNow;
    public virtual bool Revogado => RevogadoEm.HasValue;
    public virtual bool Valido => !Expirado && !Revogado;

    public virtual void Revogar()
    {
        if (RevogadoEm.HasValue) return;
        RevogadoEm = DateTime.UtcNow;
    }
}
