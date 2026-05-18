using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Salas;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class SalaRepository : ISalaRepository
{
    private readonly AppDbContext _context;

    public SalaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Sala?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.Salas
            .FirstOrDefaultAsync(s => s.Id == id && s.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteOutraComMesmoNomeNaUnidade(long estabelecimentoId, long unidadeId, string nome, long ignorarId) =>
        await _context.Salas
            .AsNoTracking()
            .AnyAsync(s => s.EstabelecimentoId == estabelecimentoId
                        && s.UnidadeId == unidadeId
                        && s.Nome.ToLower() == nome.ToLower()
                        && s.Id != ignorarId);

    public async Task Salvar(Sala sala)
    {
        if (sala.Id == 0)
        {
            await _context.Salas.AddAsync(sala);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Salas.Update(sala);
        }
    }

    public Task Excluir(Sala sala)
    {
        _context.Salas.Remove(sala);
        return Task.CompletedTask;
    }
}
