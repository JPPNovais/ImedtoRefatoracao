using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly AppDbContext _db;

    public LancamentoRepository(AppDbContext db) => _db = db;

    public async Task<Lancamento> ObterPorId(long id)
    {
        var lancamento = await _db.Lancamentos.FindAsync(id);
        if (lancamento is null)
            throw new BusinessException("Lançamento não encontrado.");
        return lancamento;
    }

    public async Task<Lancamento?> ObterPorIdOuNulo(long id) =>
        await _db.Lancamentos.FindAsync(id);

    public async Task Salvar(Lancamento lancamento)
    {
        if (lancamento.Id == 0)
            _db.Lancamentos.Add(lancamento);
        else
            _db.Lancamentos.Update(lancamento);
        await _db.SaveChangesAsync();
    }
}
