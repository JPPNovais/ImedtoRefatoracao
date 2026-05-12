using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class CategoriaFinanceiraRepository : ICategoriaFinanceiraRepository
{
    private readonly AppDbContext _db;

    public CategoriaFinanceiraRepository(AppDbContext db) => _db = db;

    public async Task<CategoriaFinanceira?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.CategoriasFinanceiras
            .FirstOrDefaultAsync(c => c.Id == id && c.EstabelecimentoId == estabelecimentoId);

    public async Task<bool> ExisteComNomeETipo(string nome, string tipo, long estabelecimentoId)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(tipo)) return false;
        if (!Enum.TryParse<TipoCategoria>(tipo, out var tipoEnum)) return false;
        var nomeNorm = nome.Trim();
        return await _db.CategoriasFinanceiras
            .AsNoTracking()
            .AnyAsync(c => c.EstabelecimentoId == estabelecimentoId
                        && c.Tipo == tipoEnum
                        && c.Nome == nomeNorm);
    }

    public async Task Salvar(CategoriaFinanceira categoria)
    {
        if (categoria.Id == 0)
            _db.CategoriasFinanceiras.Add(categoria);
        else
            _db.CategoriasFinanceiras.Update(categoria);
        await _db.SaveChangesAsync();
    }
}
