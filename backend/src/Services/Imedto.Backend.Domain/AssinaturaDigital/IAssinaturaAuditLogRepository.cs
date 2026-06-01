namespace Imedto.Backend.Domain.AssinaturaDigital;

/// <summary>
/// Repositório append-only de audit de assinatura digital.
/// Nunca permite deleção — retenção mínima 730 dias (implicação legal de documento médico assinado).
/// </summary>
public interface IAssinaturaAuditLogRepository
{
    Task SalvarAsync(AssinaturaAuditLog log, CancellationToken ct = default);
}
