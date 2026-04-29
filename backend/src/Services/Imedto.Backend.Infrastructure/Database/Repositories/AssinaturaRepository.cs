using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Assinaturas;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class AssinaturaRepository : IAssinaturaRepository
{
    private readonly AppDbContext _db;

    public AssinaturaRepository(AppDbContext db) => _db = db;

    public async Task<Assinatura?> ObterPorEstabelecimentoOuNulo(long estabelecimentoId)
        => await _db.Assinaturas.FirstOrDefaultAsync(a => a.EstabelecimentoId == estabelecimentoId);

    public async Task Salvar(Assinatura assinatura)
    {
        if (assinatura.Id == 0)
        {
            await _db.Assinaturas.AddAsync(assinatura);
            await _db.SaveChangesAsync();
        }
        else
        {
            _db.Assinaturas.Update(assinatura);
        }
    }

    public async Task<List<Assinatura>> ListarTrialsExpirando(DateTime ate)
    {
        return await _db.Assinaturas
            .Where(a => a.Status == StatusAssinatura.Trial
                        && a.ExpiraEm.HasValue
                        && a.ExpiraEm.Value <= ate)
            .ToListAsync();
    }
}
