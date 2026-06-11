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

    /// <summary>
    /// Retorna IDs de estabelecimentos que possuem vigência ativa com o plano informado.
    /// Usado para invalidação de cache em massa ao editar features/limites de um plano.
    /// </summary>
    public async Task<IReadOnlyList<long>> ListarEstabelecimentosComPlanoAtivoAsync(
        Guid planoId,
        CancellationToken ct = default)
        => await _db.ImedtoAssinaturas
            .Where(a => a.PlanoId == planoId && a.FimEm == null)
            .Select(a => a.EstabelecimentoId)
            .ToListAsync(ct);

    public void Adicionar(ImedtoAssinatura assinatura)
        => _db.ImedtoAssinaturas.Add(assinatura);

    public void Atualizar(ImedtoAssinatura assinatura)
        => _db.ImedtoAssinaturas.Update(assinatura);
}
