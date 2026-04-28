using Imedto.Backend.Domain.Inventario;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class MovimentacaoEstoqueRepository : IMovimentacaoEstoqueRepository
{
    private readonly AppDbContext _db;

    public MovimentacaoEstoqueRepository(AppDbContext db) => _db = db;

    public async Task Salvar(MovimentacaoEstoque movimentacao)
    {
        _db.MovimentacoesEstoque.Add(movimentacao);
        await _db.SaveChangesAsync();
    }
}
