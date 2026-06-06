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

    public async Task<ProntuarioVariavelPool?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _context.ProntuarioVariaveisPool
            .FirstOrDefaultAsync(p => p.Id == id && p.EstabelecimentoId == estabelecimentoId);

    /// <summary>
    /// Dedup canônica: considera padrão-sistema OU do estabelecimento.
    /// Carrega candidatos em memória e aplica NormalizadorPool para
    /// comparação insensível a acento (sem extensão unaccent no Postgres).
    /// </summary>
    public async Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, TipoVariavelPool tipo, string nome, long ignorarId)
    {
        var nomeNorm = NormalizadorPool.Normalizar(nome);
        if (string.IsNullOrEmpty(nomeNorm)) return false;

        var candidatos = await _context.ProntuarioVariaveisPool
            .AsNoTracking()
            .Where(p => (p.EhPadraoSistema || p.EstabelecimentoId == estabelecimentoId)
                     && p.Tipo == tipo
                     && p.Id != ignorarId)
            .Select(p => p.Nome)
            .ToListAsync();

        return candidatos.Any(n => NormalizadorPool.Normalizar(n) == nomeNorm);
    }

    /// <summary>
    /// Lista todos os itens ativos (padrão-sistema + do estabelecimento) de um tipo.
    /// Usado pela extração automática ao salvar evolução para dedup em memória.
    /// </summary>
    public async Task<IReadOnlyList<ProntuarioVariavelPool>> ListarAtivosPorTipo(long estabelecimentoId, TipoVariavelPool tipo) =>
        await _context.ProntuarioVariaveisPool
            .AsNoTracking()
            .Where(p => (p.EhPadraoSistema || p.EstabelecimentoId == estabelecimentoId)
                     && p.Tipo == tipo
                     && p.Ativo)
            .ToListAsync();

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
