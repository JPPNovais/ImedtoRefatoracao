using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Assinaturas;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class PlanoRepository : IPlanoRepository
{
    private readonly AppDbContext _db;

    public PlanoRepository(AppDbContext db) => _db = db;

    public async Task<Plano?> ObterPorIdOuNulo(long id)
        => await _db.Planos.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Plano?> ObterPorNomeOuNulo(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return null;
        var nomeNormalizado = nome.Trim();
        // ILIKE para casar nome do plano sem sensibilidade a caixa — útil pro seed que usa "Trial"/"trial".
        return await _db.Planos
            .FirstOrDefaultAsync(p => EF.Functions.ILike(p.Nome, nomeNormalizado));
    }

    public async Task Salvar(Plano plano)
    {
        if (plano.Id == 0)
        {
            await _db.Planos.AddAsync(plano);
            // Flush para popular o Id auto-gerado — caller pode precisar referenciar logo após.
            await _db.SaveChangesAsync();
        }
        else
        {
            _db.Planos.Update(plano);
        }
    }
}
