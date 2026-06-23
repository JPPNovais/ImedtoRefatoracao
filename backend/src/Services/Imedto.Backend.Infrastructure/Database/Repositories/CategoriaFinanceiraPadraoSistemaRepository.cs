using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório de escrita para o catálogo global de categorias financeiras padrão (admin global).
/// Briefing 2026-06-22_003 — M2/M3.
/// </summary>
public class CategoriaFinanceiraPadraoSistemaRepository : ICategoriaFinanceiraPadraoSistemaRepository
{
    private readonly AppDbContext _db;

    public CategoriaFinanceiraPadraoSistemaRepository(AppDbContext db) => _db = db;

    public async Task<CategoriaFinanceiraPadraoSistema?> ObterPorIdOuNulo(long id)
        => await _db.CategoriasFinanceirasPadraoSistema.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> ExisteGlobalComNomeETipo(string nome, TipoCategoria tipo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nome)) return false;
        var nomeNorm = nome.Trim();
        return await _db.CategoriasFinanceirasPadraoSistema
            .AsNoTracking()
            .AnyAsync(c => c.Nome == nomeNorm && c.Tipo == tipo, ct);
    }

    public async Task<IReadOnlyList<CategoriaFinanceiraPadraoSistema>> ListarAtivas(CancellationToken ct = default)
        => await _db.CategoriasFinanceirasPadraoSistema
            .AsNoTracking()
            .Where(c => c.Ativo)
            .ToListAsync(ct);

    public async Task Salvar(CategoriaFinanceiraPadraoSistema categoria, CancellationToken ct = default)
    {
        if (categoria.Id == 0)
            _db.CategoriasFinanceirasPadraoSistema.Add(categoria);
        else
            _db.CategoriasFinanceirasPadraoSistema.Update(categoria);
        await _db.SaveChangesAsync(ct);
    }
}
