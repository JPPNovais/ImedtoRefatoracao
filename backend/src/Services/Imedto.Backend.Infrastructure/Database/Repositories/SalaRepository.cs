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

    public async Task<Sala> ObterPorId(long id)
    {
        var sala = await _context.Salas.FindAsync(id);
        if (sala is null)
            throw new KeyNotFoundException($"Repartição {id} não encontrada.");
        return sala;
    }

    public async Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, string nome, long ignorarId) =>
        await _context.Salas
            .AsNoTracking()
            .AnyAsync(s => s.EstabelecimentoId == estabelecimentoId
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
