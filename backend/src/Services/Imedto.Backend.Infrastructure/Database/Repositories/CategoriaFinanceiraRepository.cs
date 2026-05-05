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

    public async Task Salvar(CategoriaFinanceira categoria)
    {
        if (categoria.Id == 0)
            _db.CategoriasFinanceiras.Add(categoria);
        else
            _db.CategoriasFinanceiras.Update(categoria);
        await _db.SaveChangesAsync();
    }
}
