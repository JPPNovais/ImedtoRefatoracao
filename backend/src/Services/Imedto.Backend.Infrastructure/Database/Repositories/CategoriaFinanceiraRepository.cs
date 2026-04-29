using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class CategoriaFinanceiraRepository : ICategoriaFinanceiraRepository
{
    private readonly AppDbContext _db;

    public CategoriaFinanceiraRepository(AppDbContext db) => _db = db;

    public async Task<CategoriaFinanceira> ObterPorId(long id)
    {
        var categoria = await _db.CategoriasFinanceiras.FindAsync(id);
        if (categoria is null)
            throw new BusinessException("Categoria não encontrada.");
        return categoria;
    }

    public async Task<CategoriaFinanceira?> ObterPorIdOuNulo(long id)
        => await _db.CategoriasFinanceiras.FindAsync(id);

    public async Task Salvar(CategoriaFinanceira categoria)
    {
        if (categoria.Id == 0)
            _db.CategoriasFinanceiras.Add(categoria);
        else
            _db.CategoriasFinanceiras.Update(categoria);
        await _db.SaveChangesAsync();
    }
}
