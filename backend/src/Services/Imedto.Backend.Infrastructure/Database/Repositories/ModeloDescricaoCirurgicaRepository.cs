using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloDescricaoCirurgicaRepository : IModeloDescricaoCirurgicaRepository
{
    private readonly AppDbContext _context;

    public ModeloDescricaoCirurgicaRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Falha-fechada: filtra por estabelecimentoId. Modelos padrão-sistema (NULL) nunca retornados.
    /// </summary>
    public async Task<ModeloDescricaoCirurgica?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.ModelosDescricaoCirurgica
            .FirstOrDefaultAsync(m => m.Id == id && m.EstabelecimentoId == estabelecimentoId);

    /// <summary>
    /// Dedup: considera padrão-sistema OU do estabelecimento no escopo visível ao tenant.
    /// Carrega candidatos em memória e aplica NormalizadorPool (sem unaccent no Postgres).
    /// </summary>
    public async Task<bool> ExisteOutroComMesmoTitulo(long estabelecimentoId, string titulo, long ignorarId)
    {
        var tituloNorm = NormalizadorPool.Normalizar(titulo);
        if (string.IsNullOrEmpty(tituloNorm)) return false;

        var candidatos = await _context.ModelosDescricaoCirurgica
            .AsNoTracking()
            .Where(m => (m.EhPadraoSistema || m.EstabelecimentoId == estabelecimentoId)
                     && m.Id != ignorarId
                     && m.Ativo)
            .Select(m => m.Titulo)
            .ToListAsync();

        return candidatos.Any(t => NormalizadorPool.Normalizar(t) == tituloNorm);
    }

    public async Task Salvar(ModeloDescricaoCirurgica modelo)
    {
        if (modelo.Id == 0)
        {
            await _context.ModelosDescricaoCirurgica.AddAsync(modelo);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ModelosDescricaoCirurgica.Update(modelo);
        }
    }

    public Task Excluir(ModeloDescricaoCirurgica modelo)
    {
        _context.ModelosDescricaoCirurgica.Remove(modelo);
        return Task.CompletedTask;
    }
}
