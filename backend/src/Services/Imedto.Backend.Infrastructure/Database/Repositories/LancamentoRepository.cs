using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly AppDbContext _db;

    public LancamentoRepository(AppDbContext db) => _db = db;

    public async Task<Lancamento?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _db.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == id && l.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(Lancamento lancamento)
    {
        if (lancamento.Id == 0)
            _db.Lancamentos.Add(lancamento);
        else
            _db.Lancamentos.Update(lancamento);
        await _db.SaveChangesAsync();
    }
}
