using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Refresh token do administrador global. Armazena apenas o hash (SHA-256) do token —
/// nunca o token em claro. Rotacionado a cada uso. Revogado no logout e na desativação do admin.
/// </summary>
public class ImedtoAdminRefreshToken : Entity<Guid>
{
    public virtual Guid AdminId { get; protected set; }
    public virtual string TokenHash { get; protected set; } = string.Empty;
    public virtual DateTimeOffset ExpiraEm { get; protected set; }
    public virtual DateTimeOffset? RevogadoEm { get; protected set; }
    public virtual DateTimeOffset CriadoEm { get; protected set; }
    public virtual string? IpOrigem { get; protected set; }
    public virtual string? UserAgent { get; protected set; }

    protected ImedtoAdminRefreshToken() { }

    public static ImedtoAdminRefreshToken Criar(
        Guid adminId,
        string tokenHash,
        DateTimeOffset expiraEm,
        string? ipOrigem,
        string? userAgent)
    {
        if (adminId == Guid.Empty)
            throw new BusinessException("AdminId inválido.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new BusinessException("Hash do token é obrigatório.");
        if (expiraEm <= DateTimeOffset.UtcNow)
            throw new BusinessException("Data de expiração deve ser futura.");

        return new ImedtoAdminRefreshToken
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            TokenHash = tokenHash,
            ExpiraEm = expiraEm,
            CriadoEm = DateTimeOffset.UtcNow,
            IpOrigem = ipOrigem,
            UserAgent = userAgent
        };
    }

    public bool EstaAtivo() => RevogadoEm is null && ExpiraEm > DateTimeOffset.UtcNow;

    public virtual void Revogar()
    {
        if (RevogadoEm is not null) return; // idempotente
        RevogadoEm = DateTimeOffset.UtcNow;
    }
}
