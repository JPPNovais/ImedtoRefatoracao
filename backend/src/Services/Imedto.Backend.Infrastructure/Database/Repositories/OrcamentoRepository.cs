using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class OrcamentoRepository : IOrcamentoRepository
{
    private readonly AppDbContext _db;

    public OrcamentoRepository(AppDbContext db) => _db = db;

    public async Task<Orcamento?> ObterPorIdOuNulo(long id, long estabelecimentoId) =>
        await _db.Orcamentos
            .FirstOrDefaultAsync(o => o.Id == id && o.EstabelecimentoId == estabelecimentoId);

    public async Task<Orcamento?> ObterPorIdCompletoOuNulo(long id, long estabelecimentoId)
    {
        // AsSplitQuery evita explosão cartesiana — Postgres faz uma SELECT por collection
        // (1 root + 5 collections + 2 relações 1:1) em vez de uma só com produto cartesiano.
        return await _db.Orcamentos
            .Include(o => o.Itens)
            .Include(o => o.Equipe)
            .Include(o => o.Implantes)
            .Include(o => o.FormasPagamento)
            .Include(o => o.Cirurgias)
            .Include(o => o.Internacao)
            .Include(o => o.Anestesia)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id && o.EstabelecimentoId == estabelecimentoId);
    }

    public async Task Salvar(Orcamento orcamento)
    {
        if (orcamento.Id == 0)
            _db.Orcamentos.Add(orcamento);
        else
            _db.Orcamentos.Update(orcamento);
        await _db.SaveChangesAsync();
    }
}
