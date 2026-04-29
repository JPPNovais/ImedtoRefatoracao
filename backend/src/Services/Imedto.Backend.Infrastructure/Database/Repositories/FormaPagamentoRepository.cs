using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class FormaPagamentoRepository : IFormaPagamentoRepository
{
    private readonly AppDbContext _db;

    public FormaPagamentoRepository(AppDbContext db) => _db = db;

    public async Task<FormaPagamento> ObterPorId(long id)
    {
        var forma = await _db.FormasPagamento.FindAsync(id);
        if (forma is null)
            throw new BusinessException("Forma de pagamento não encontrada.");
        return forma;
    }

    public async Task<FormaPagamento?> ObterPorIdOuNulo(long id)
        => await _db.FormasPagamento.FindAsync(id);

    public async Task Salvar(FormaPagamento forma)
    {
        if (forma.Id == 0)
            _db.FormasPagamento.Add(forma);
        else
            _db.FormasPagamento.Update(forma);
        await _db.SaveChangesAsync();
    }
}
