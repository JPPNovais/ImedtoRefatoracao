using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProntuarioVariavelPoolRepository : IProntuarioVariavelPoolRepository
{
    private readonly AppDbContext _context;

    public ProntuarioVariavelPoolRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProntuarioVariavelPool> ObterPorId(long id)
    {
        var p = await _context.ProntuarioVariaveisPool.FindAsync(id);
        if (p is null) throw new KeyNotFoundException($"Item do pool {id} não encontrado.");
        return p;
    }

    public async Task<ProntuarioVariavelPool> ObterPorIdOuNulo(long id) =>
        await _context.ProntuarioVariaveisPool.FindAsync(id);

    public async Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, TipoVariavelPool tipo, string nome, long ignorarId) =>
        await _context.ProntuarioVariaveisPool
            .AsNoTracking()
            .AnyAsync(p => p.EstabelecimentoId == estabelecimentoId
                        && p.Tipo == tipo
                        && p.Nome.ToLower() == nome.ToLower()
                        && p.Id != ignorarId);

    public async Task Salvar(ProntuarioVariavelPool item)
    {
        if (item.Id == 0)
        {
            await _context.ProntuarioVariaveisPool.AddAsync(item);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ProntuarioVariaveisPool.Update(item);
        }
    }

    public Task Excluir(ProntuarioVariavelPool item)
    {
        _context.ProntuarioVariaveisPool.Remove(item);
        return Task.CompletedTask;
    }
}
