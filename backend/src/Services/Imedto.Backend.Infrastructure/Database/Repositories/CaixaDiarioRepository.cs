using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class CaixaDiarioRepository : ICaixaDiarioRepository
{
    private readonly AppDbContext _db;

    public CaixaDiarioRepository(AppDbContext db) => _db = db;

    public async Task<CaixaDiario?> ObterPorData(long estabelecimentoId, DateOnly data) =>
        await _db.CaixasDiario
            .FirstOrDefaultAsync(c => c.EstabelecimentoId == estabelecimentoId && c.Data == data);

    public async Task Salvar(CaixaDiario caixa)
    {
        if (caixa.Id == 0)
            _db.CaixasDiario.Add(caixa);
        else
            _db.CaixasDiario.Update(caixa);
        await _db.SaveChangesAsync();
    }
}
