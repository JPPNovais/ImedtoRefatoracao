using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

public class ImedtoPlanoRepository : IImedtoPlanoRepository
{
    private readonly AppDbContext _db;

    public ImedtoPlanoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ImedtoPlano?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ImedtoPlanos.FindAsync([id], ct);

    public async Task<bool> ExisteNomeAsync(string nome, Guid? excluindoId = null, CancellationToken ct = default)
    {
        var nomeNormalizado = nome.Trim().ToLowerInvariant();
        return await _db.ImedtoPlanos
            .AnyAsync(p => p.Nome.ToLower() == nomeNormalizado && p.Id != excluindoId, ct);
    }

    public void Adicionar(ImedtoPlano plano)
        => _db.ImedtoPlanos.Add(plano);

    public void Atualizar(ImedtoPlano plano)
        => _db.ImedtoPlanos.Update(plano);
}
