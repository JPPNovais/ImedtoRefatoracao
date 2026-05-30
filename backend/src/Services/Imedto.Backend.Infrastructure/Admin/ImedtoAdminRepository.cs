using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Repositório de escrita (EF Core) para <see cref="ImedtoAdmin"/> e tokens relacionados.
/// Queries de leitura admin usam Dapper em AdminQueryRepository (criado pelos devs de feature).
/// </summary>
public class ImedtoAdminRepository
{
    private readonly AppDbContext _db;

    public ImedtoAdminRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ImedtoAdmin?> ObterPorEmailAsync(string email, CancellationToken ct = default)
        => await _db.ImedtoAdmins
            .FirstOrDefaultAsync(a => a.Email == email.ToLowerInvariant(), ct);

    public async Task<ImedtoAdmin?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ImedtoAdmins.FindAsync([id], ct);

    public async Task<int> ContarAtivosAsync(CancellationToken ct = default)
        => await _db.ImedtoAdmins.CountAsync(a => a.Ativo, ct);

    public async Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default)
        => await _db.ImedtoAdmins.AnyAsync(a => a.Email == email.ToLowerInvariant(), ct);

    public void Adicionar(ImedtoAdmin admin)
        => _db.ImedtoAdmins.Add(admin);

    public void Atualizar(ImedtoAdmin admin)
        => _db.ImedtoAdmins.Update(admin);
}

/// <summary>
/// Repositório de refresh tokens admin.
/// </summary>
public class ImedtoAdminRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public ImedtoAdminRefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ImedtoAdminRefreshToken?> ObterPorHashAsync(string hash, CancellationToken ct = default)
        => await _db.ImedtoAdminRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevogadoEm == null, ct);

    public void Adicionar(ImedtoAdminRefreshToken token)
        => _db.ImedtoAdminRefreshTokens.Add(token);

    public void Atualizar(ImedtoAdminRefreshToken token)
        => _db.ImedtoAdminRefreshTokens.Update(token);

    /// <summary>Revoga todos os refresh tokens de um admin (usado no logout e desativação).</summary>
    public async Task RevogarTodosDoAdminAsync(Guid adminId, CancellationToken ct = default)
    {
        var agora = DateTimeOffset.UtcNow;
        var tokens = await _db.ImedtoAdminRefreshTokens
            .Where(t => t.AdminId == adminId && t.RevogadoEm == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.Revogar();
    }
}
