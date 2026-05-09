using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositórios EF para o domínio de Auth local. Os 3 ficam aqui porque são
/// triviais e compartilham contexto de uso (LocalJwtAuthService).
/// </summary>
public class EfAuthCredencialRepository : IAuthCredencialRepository
{
    private readonly AppDbContext _context;
    public EfAuthCredencialRepository(AppDbContext context) => _context = context;

    public Task<AuthCredencial> ObterPorEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<AuthCredencial>(null);
        var emailNorm = email.Trim().ToLowerInvariant();
        // citext: comparação case-insensitive nativa.
        return _context.AuthCredenciais.FirstOrDefaultAsync(c => c.Email == emailNorm);
    }

    public Task<AuthCredencial> ObterPorIdAsync(Guid usuarioId) =>
        _context.AuthCredenciais.FirstOrDefaultAsync(c => c.Id == usuarioId);

    public async Task<bool> ExisteParaEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var emailNorm = email.Trim().ToLowerInvariant();
        return await _context.AuthCredenciais.AnyAsync(c => c.Email == emailNorm);
    }

    public async Task AdicionarAsync(AuthCredencial credencial) =>
        await _context.AuthCredenciais.AddAsync(credencial);

    public void Atualizar(AuthCredencial credencial) =>
        _context.AuthCredenciais.Update(credencial);

    public void Remover(AuthCredencial credencial) =>
        _context.AuthCredenciais.Remove(credencial);
}

public class EfAuthRefreshTokenRepository : IAuthRefreshTokenRepository
{
    private readonly AppDbContext _context;
    public EfAuthRefreshTokenRepository(AppDbContext context) => _context = context;

    public Task<AuthRefreshToken> ObterPorHashAsync(string tokenHash) =>
        _context.AuthRefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task AdicionarAsync(AuthRefreshToken token) =>
        await _context.AuthRefreshTokens.AddAsync(token);

    public void Atualizar(AuthRefreshToken token) =>
        _context.AuthRefreshTokens.Update(token);

    public async Task RevogarTodosDoUsuarioAsync(Guid usuarioId)
    {
        var ativos = await _context.AuthRefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null)
            .ToListAsync();
        foreach (var t in ativos) t.Revogar();
    }
}

public class EfAuthEmailTokenRepository : IAuthEmailTokenRepository
{
    private readonly AppDbContext _context;
    public EfAuthEmailTokenRepository(AppDbContext context) => _context = context;

    public Task<AuthEmailToken> ObterValidoPorHashAsync(string tokenHash, AuthEmailTokenTipo tipo) =>
        _context.AuthEmailTokens.FirstOrDefaultAsync(t =>
            t.TokenHash == tokenHash && t.Tipo == tipo);

    public Task<AuthEmailToken> ObterUltimoCriadoAsync(Guid usuarioId, AuthEmailTokenTipo tipo) =>
        _context.AuthEmailTokens
            .Where(t => t.UsuarioId == usuarioId && t.Tipo == tipo)
            .OrderByDescending(t => t.CriadoEm)
            .FirstOrDefaultAsync();

    public async Task AdicionarAsync(AuthEmailToken token) =>
        await _context.AuthEmailTokens.AddAsync(token);

    public void Atualizar(AuthEmailToken token) =>
        _context.AuthEmailTokens.Update(token);
}
