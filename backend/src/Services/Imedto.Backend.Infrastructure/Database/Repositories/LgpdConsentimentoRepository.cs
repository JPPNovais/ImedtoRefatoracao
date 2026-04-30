using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Lgpd;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class LgpdConsentimentoRepository : ILgpdConsentimentoRepository
{
    private readonly AppDbContext _db;

    public LgpdConsentimentoRepository(AppDbContext db) => _db = db;

    public async Task Salvar(LgpdConsentimento consentimento)
    {
        _db.LgpdConsentimentos.Add(consentimento);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<LgpdConsentimento>> ListarPorUsuario(Guid usuarioId) =>
        await _db.LgpdConsentimentos
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.AceitoEm)
            .ToListAsync();
}
