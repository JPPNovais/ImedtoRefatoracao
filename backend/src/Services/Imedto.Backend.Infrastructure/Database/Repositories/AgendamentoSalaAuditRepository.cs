using Imedto.Backend.Domain.Agendamentos;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class AgendamentoSalaAuditRepository : IAgendamentoSalaAuditRepository
{
    private readonly AppDbContext _db;

    public AgendamentoSalaAuditRepository(AppDbContext db) => _db = db;

    public async Task Registrar(AgendamentoSalaAudit audit)
    {
        await _db.AgendamentoSalaAudits.AddAsync(audit);
        await _db.SaveChangesAsync();
    }
}
