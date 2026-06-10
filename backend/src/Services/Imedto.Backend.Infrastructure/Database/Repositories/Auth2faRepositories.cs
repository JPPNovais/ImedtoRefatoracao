using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositórios EF para as entidades de 2FA TOTP:
/// <see cref="Usuario2fa"/>, <see cref="Usuario2faCodigoRecuperacao"/> e
/// <see cref="UsuarioSegurancaAudit"/> (append-only).
/// </summary>
public class EfUsuario2faRepository : IUsuario2faRepository
{
    private readonly AppDbContext _context;
    public EfUsuario2faRepository(AppDbContext context) => _context = context;

    public Task<Usuario2fa> ObterPorUsuarioId(Guid usuarioId) =>
        // PK da entidade = usuario_id (Id no EF)
        _context.Usuario2fas.FirstOrDefaultAsync(u => u.Id == usuarioId);

    public async Task Adicionar(Usuario2fa estado) =>
        await _context.Usuario2fas.AddAsync(estado);

    public void Atualizar(Usuario2fa estado) =>
        _context.Usuario2fas.Update(estado);

    public void Remover(Usuario2fa estado) =>
        _context.Usuario2fas.Remove(estado);
}

public class EfUsuario2faCodigoRecuperacaoRepository : IUsuario2faCodigoRecuperacaoRepository
{
    private readonly AppDbContext _context;
    public EfUsuario2faCodigoRecuperacaoRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Usuario2faCodigoRecuperacao>> ListarPorUsuario(Guid usuarioId) =>
        await _context.Usuario2faCodigosRecuperacao
            .Where(c => c.UsuarioId == usuarioId)
            .ToListAsync();

    public async Task Adicionar(Usuario2faCodigoRecuperacao codigo) =>
        await _context.Usuario2faCodigosRecuperacao.AddAsync(codigo);

    public void Atualizar(Usuario2faCodigoRecuperacao codigo) =>
        _context.Usuario2faCodigosRecuperacao.Update(codigo);

    public async Task RemoverTodosDoUsuario(Guid usuarioId)
    {
        var codigos = await _context.Usuario2faCodigosRecuperacao
            .Where(c => c.UsuarioId == usuarioId)
            .ToListAsync();
        _context.Usuario2faCodigosRecuperacao.RemoveRange(codigos);
    }
}

public class EfUsuarioSegurancaAuditRepository : IUsuarioSegurancaAuditRepository
{
    private readonly AppDbContext _context;
    public EfUsuarioSegurancaAuditRepository(AppDbContext context) => _context = context;

    public async Task Adicionar(UsuarioSegurancaAudit auditoria) =>
        await _context.UsuarioSegurancaAudits.AddAsync(auditoria);
}
