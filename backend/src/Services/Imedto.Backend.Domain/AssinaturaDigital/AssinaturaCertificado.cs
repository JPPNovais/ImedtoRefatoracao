using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.AssinaturaDigital;

/// <summary>
/// Aggregate root — vínculo entre um médico (usuário) e seu certificado em nuvem ICP-Brasil.
/// O vínculo é por conta de usuário (não por estabelecimento): um médico que atua em N
/// estabelecimentos vincula uma vez e pode assinar em todos.
///
/// O <see cref="RefreshToken"/> é sempre armazenado cifrado via IDataProtectionProvider —
/// nunca em claro no banco. Expor o token decifrado é responsabilidade exclusiva do
/// <c>AssinaturaCertificadoRepository</c> para uso interno do provider.
/// </summary>
public class AssinaturaCertificado : Entity<Guid>
{
    public virtual Guid MedicoId { get; protected set; }
    public virtual string Provedor { get; protected set; }
    /// <summary>
    /// Refresh token cifrado com IDataProtectionProvider antes de persistir.
    /// Nunca retornar em payload de leitura — apenas para uso interno do provider.
    /// </summary>
    public virtual string RefreshToken { get; protected set; }
    public virtual DateTime? ExpiraEm { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected AssinaturaCertificado() { }

    public static AssinaturaCertificado Vincular(
        Guid medicoId,
        string provedor,
        string refreshTokenCifrado,
        DateTime? expiraEm)
    {
        if (medicoId == Guid.Empty)
            throw new BusinessException("Médico é obrigatório.");
        if (string.IsNullOrWhiteSpace(provedor))
            throw new BusinessException("Provedor de assinatura é obrigatório.");
        if (string.IsNullOrWhiteSpace(refreshTokenCifrado))
            throw new BusinessException("Token do certificado é obrigatório.");

        return new AssinaturaCertificado
        {
            Id = Guid.NewGuid(),
            MedicoId = medicoId,
            Provedor = provedor.Trim(),
            RefreshToken = refreshTokenCifrado,
            ExpiraEm = expiraEm,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual void AtualizarToken(string refreshTokenCifrado, DateTime? expiraEm)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenCifrado))
            throw new BusinessException("Token do certificado é obrigatório.");

        RefreshToken = refreshTokenCifrado;
        ExpiraEm = expiraEm;
    }
}
