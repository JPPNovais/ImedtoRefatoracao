using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Persiste cada chamada à IA. Usa o DbContext da request porque a operação é
/// transacional junto com o SaveChanges do handler — mas no decorator chamamos
/// <c>SaveChangesAsync</c> diretamente para que a auditoria seja gravada mesmo
/// quando a chamada IA falhou ou foi cache-hit (não há aggregate de domínio
/// participando do mesmo UoW).
/// </summary>
public class AiAuditRepository : IAiAuditRepository
{
    private readonly AppDbContext _context;

    public AiAuditRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(AiAuditLog log, CancellationToken ct = default)
    {
        await _context.AiAuditLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }
}
