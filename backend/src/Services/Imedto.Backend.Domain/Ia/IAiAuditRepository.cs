namespace Imedto.Backend.Domain.Ia;

public interface IAiAuditRepository
{
    Task RegistrarAsync(AiAuditLog log, CancellationToken ct = default);
}
