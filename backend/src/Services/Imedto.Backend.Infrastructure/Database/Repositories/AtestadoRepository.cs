using Imedto.Backend.Domain.Atestados;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class AtestadoRepository : IAtestadoRepository
{
    private readonly AppDbContext _context;

    public AtestadoRepository(AppDbContext context) => _context = context;

    public async Task<Atestado?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.Atestados
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.EstabelecimentoId == estabelecimentoId &&
                a.DeletadoEm == null);

    public async Task Salvar(Atestado atestado)
    {
        if (atestado.Id == 0)
        {
            await _context.Atestados.AddAsync(atestado);
            // SaveChanges aqui resolve o Id — necessário para o evento AtestadoEmitido.
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Atestados.Update(atestado);
        }
    }
}

public class ModeloAtestadoRepository : IModeloAtestadoRepository
{
    private readonly AppDbContext _context;

    public ModeloAtestadoRepository(AppDbContext context) => _context = context;

    public async Task<ModeloAtestado?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.ModelosAtestado
            .FirstOrDefaultAsync(m => m.Id == id && m.EstabelecimentoId == estabelecimentoId);

    public async Task<IReadOnlyList<ModeloAtestado>> ListarPorEstabelecimento(long estabelecimentoId) =>
        await _context.ModelosAtestado
            .Where(m => m.EstabelecimentoId == estabelecimentoId)
            .OrderBy(m => m.Nome)
            .ToListAsync();

    public async Task Salvar(ModeloAtestado modelo)
    {
        if (modelo.Id == 0)
        {
            await _context.ModelosAtestado.AddAsync(modelo);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ModelosAtestado.Update(modelo);
        }
    }

    public Task Excluir(ModeloAtestado modelo)
    {
        _context.ModelosAtestado.Remove(modelo);
        return Task.CompletedTask;
    }
}
