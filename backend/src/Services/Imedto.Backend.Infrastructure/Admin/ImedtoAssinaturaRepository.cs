using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

public class ImedtoAssinaturaRepository : IImedtoAssinaturaRepository
{
    private readonly AppDbContext _db;

    public ImedtoAssinaturaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ImedtoAssinatura?> ObterVigenteDoEstabelecimentoAsync(
        long estabelecimentoId,
        CancellationToken ct = default)
        => await _db.ImedtoAssinaturas
            .FirstOrDefaultAsync(a => a.EstabelecimentoId == estabelecimentoId && a.FimEm == null, ct);

    public async Task<ImedtoAssinatura?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ImedtoAssinaturas.FindAsync([id], ct);

    public void Adicionar(ImedtoAssinatura assinatura)
        => _db.ImedtoAssinaturas.Add(assinatura);

    public void Atualizar(ImedtoAssinatura assinatura)
        => _db.ImedtoAssinaturas.Update(assinatura);
}
