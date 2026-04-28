using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class OrcamentoRepository : IOrcamentoRepository
{
    private readonly AppDbContext _db;

    public OrcamentoRepository(AppDbContext db) => _db = db;

    public async Task<Orcamento> ObterPorId(long id)
    {
        var orc = await _db.Orcamentos.FindAsync(id);
        if (orc is null)
            throw new BusinessException("Orçamento não encontrado.");
        return orc;
    }

    public async Task<Orcamento> ObterPorIdComItens(long id)
    {
        var orc = await _db.Orcamentos
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (orc is null)
            throw new BusinessException("Orçamento não encontrado.");
        return orc;
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
