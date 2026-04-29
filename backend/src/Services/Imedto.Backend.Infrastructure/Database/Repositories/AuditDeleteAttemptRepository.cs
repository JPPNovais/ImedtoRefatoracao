using Imedto.Backend.Domain.Auditoria;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class AuditDeleteAttemptRepository : IAuditDeleteAttemptRepository
{
    private readonly AppDbContext _context;

    public AuditDeleteAttemptRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task Salvar(AuditDeleteAttempt registro)
    {
        await _context.AuditDeleteAttempts.AddAsync(registro);
        await _context.SaveChangesAsync();
    }
}
