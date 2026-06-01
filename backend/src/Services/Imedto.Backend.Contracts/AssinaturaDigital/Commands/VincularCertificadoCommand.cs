using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.AssinaturaDigital.Commands;

/// <summary>
/// Vincula um certificado em nuvem ICP-Brasil ao médico autenticado.
/// O refresh token chega em claro do frontend e é cifrado pelo handler antes de persistir.
/// </summary>
public class VincularCertificadoCommand : ICommand
{
    public Guid MedicoId { get; set; }
    /// <summary>"BirdId" | "VIDaaS".</summary>
    public string Provedor { get; set; } = "BirdId";
    /// <summary>Refresh token em claro — cifrado pelo handler via IDataProtectionProvider.</summary>
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? ExpiraEm { get; set; }
}
