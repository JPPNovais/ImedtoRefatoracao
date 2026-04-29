namespace Imedto.Backend.Domain.Auditoria;

public interface IAuditDeleteAttemptRepository
{
    Task Salvar(AuditDeleteAttempt registro);
}
