using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Usuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> ObterPorId(Guid id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
            throw new KeyNotFoundException($"Usuário {id} não encontrado.");
        return usuario;
    }

    public async Task<Usuario> ObterPorIdOuNulo(Guid id) =>
        await _context.Usuarios.FindAsync(id);

    public async Task<bool> ExisteCpf(string cpf, Guid ignorarUsuarioId) =>
        await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(u => u.Cpf == cpf && u.Id != ignorarUsuarioId);

    public async Task Salvar(Usuario usuario)
    {
        var jaExiste = await _context.Usuarios.AsNoTracking().AnyAsync(u => u.Id == usuario.Id);
        if (!jaExiste)
            await _context.Usuarios.AddAsync(usuario);
        else
            _context.Usuarios.Update(usuario);
    }
}
