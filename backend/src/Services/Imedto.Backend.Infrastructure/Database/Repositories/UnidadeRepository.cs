using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Unidades;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class UnidadeRepository : IUnidadeRepository
{
    private readonly AppDbContext _context;

    public UnidadeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UnidadeEstabelecimento> ObterPorId(long id)
    {
        var u = await _context.Unidades.FindAsync(id);
        if (u is null)
            throw new KeyNotFoundException($"Unidade {id} não encontrada.");
        return u;
    }

    public async Task<UnidadeEstabelecimento> ObterPorIdOuNulo(long id) =>
        await _context.Unidades.FindAsync(id);

    public async Task<IReadOnlyList<UnidadeEstabelecimento>> ListarPorEstabelecimento(long estabelecimentoId) =>
        await _context.Unidades
            .Where(u => u.EstabelecimentoId == estabelecimentoId)
            .OrderByDescending(u => u.IsPrincipal)
            .ThenBy(u => u.Nome)
            .ToListAsync();

    public async Task<UnidadeEstabelecimento> ObterPrincipalDoEstabelecimento(long estabelecimentoId) =>
        await _context.Unidades
            .FirstOrDefaultAsync(u => u.EstabelecimentoId == estabelecimentoId && u.IsPrincipal);

    public async Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, string nome, long ignorarUnidadeId) =>
        await _context.Unidades
            .AsNoTracking()
            .AnyAsync(u => u.EstabelecimentoId == estabelecimentoId
                        && u.Nome.ToLower() == nome.ToLower()
                        && u.Id != ignorarUnidadeId);

    public async Task Salvar(UnidadeEstabelecimento unidade)
    {
        if (unidade.Id == 0)
        {
            await _context.Unidades.AddAsync(unidade);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Unidades.Update(unidade);
        }
    }

    public Task Excluir(UnidadeEstabelecimento unidade)
    {
        _context.Unidades.Remove(unidade);
        return Task.CompletedTask;
    }
}
