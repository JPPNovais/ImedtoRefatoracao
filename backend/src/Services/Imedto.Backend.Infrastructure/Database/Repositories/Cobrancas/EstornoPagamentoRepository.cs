using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;

public class EstornoPagamentoRepository : IEstornoPagamentoRepository
{
    private readonly AppDbContext _db;

    public EstornoPagamentoRepository(AppDbContext db) => _db = db;

    public async Task Salvar(EstornoPagamento estorno)
    {
        if (estorno.Id == 0)
            _db.EstornosPagamento.Add(estorno);
        else
            _db.EstornosPagamento.Update(estorno);
        await _db.SaveChangesAsync();
    }
}
