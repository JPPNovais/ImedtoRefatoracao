using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Idempotency;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly AppDbContext _context;

    public IdempotencyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyKey?> ObterPorKey(string key) =>
        await _context.IdempotencyKeys.FindAsync(key);

    public async Task Salvar(IdempotencyKey k)
    {
        var jaExiste = await _context.IdempotencyKeys
            .AsNoTracking()
            .AnyAsync(x => x.Key == k.Key);

        if (!jaExiste)
            await _context.IdempotencyKeys.AddAsync(k);
        else
            _context.IdempotencyKeys.Update(k);

        await _context.SaveChangesAsync();
    }

    public async Task RemoverExpiradosAsync()
    {
        var expirados = await _context.IdempotencyKeys
            .Where(k => k.ExpiraEm < DateTime.UtcNow)
            .ToListAsync();

        if (expirados.Count > 0)
        {
            _context.IdempotencyKeys.RemoveRange(expirados);
            await _context.SaveChangesAsync();
        }
    }
}
