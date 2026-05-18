namespace Imedto.Backend.Domain.Agendamentos;

public interface IAgendamentoSalaAuditRepository
{
    Task Registrar(AgendamentoSalaAudit audit);
}
