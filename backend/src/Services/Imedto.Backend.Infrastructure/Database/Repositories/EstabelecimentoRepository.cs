using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Estabelecimentos;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class EstabelecimentoRepository : IEstabelecimentoRepository
{
    private readonly AppDbContext _context;

    public EstabelecimentoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Estabelecimento> ObterPorId(long id)
    {
        var estab = await _context.Estabelecimentos.FindAsync(id);
        if (estab is null)
            throw new KeyNotFoundException($"Estabelecimento {id} não encontrado.");
        return estab;
    }

    public async Task<Estabelecimento> ObterPorIdOuNulo(long id) =>
        await _context.Estabelecimentos.FindAsync(id);

    public async Task<bool> ExisteCnpj(string cnpj, long ignorarEstabelecimentoId) =>
        await _context.Estabelecimentos
            .AsNoTracking()
            .AnyAsync(e => e.Cnpj == cnpj && e.Id != ignorarEstabelecimentoId);

    public async Task<bool> UsuarioJaEhDono(Guid usuarioId) =>
        await _context.Estabelecimentos
            .AsNoTracking()
            .AnyAsync(e => e.DonoUsuarioId == usuarioId);

    public async Task Salvar(Estabelecimento estabelecimento)
    {
        if (estabelecimento.Id == 0)
        {
            await _context.Estabelecimentos.AddAsync(estabelecimento);
            // Flush imediato para popular o Id auto-gerado, permitindo que eventos
            // e side-effects posteriores referenciem o aggregate corretamente.
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Estabelecimentos.Update(estabelecimento);
        }
    }
}
