using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Profissionais;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProfissionalRepository : IProfissionalRepository
{
    private readonly AppDbContext _context;

    public ProfissionalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Profissional> ObterPorId(Guid usuarioId)
    {
        var prof = await _context.Profissionais.FindAsync(usuarioId);
        if (prof is null)
            throw new KeyNotFoundException($"Profissional {usuarioId} não encontrado.");
        return prof;
    }

    public async Task<Profissional> ObterPorIdOuNulo(Guid usuarioId) =>
        await _context.Profissionais.FindAsync(usuarioId);

    public async Task<bool> ExisteConselhoRegistro(string conselho, string uf, string numeroRegistro, Guid ignorarUsuarioId) =>
        await _context.Profissionais
            .AsNoTracking()
            .AnyAsync(p =>
                p.Conselho == conselho &&
                p.Uf == uf &&
                p.NumeroRegistro == numeroRegistro &&
                p.Id != ignorarUsuarioId);

    public async Task Salvar(Profissional profissional)
    {
        var jaExiste = await _context.Profissionais.AsNoTracking().AnyAsync(p => p.Id == profissional.Id);
        if (!jaExiste)
            await _context.Profissionais.AddAsync(profissional);
        else
            _context.Profissionais.Update(profissional);
    }
}
