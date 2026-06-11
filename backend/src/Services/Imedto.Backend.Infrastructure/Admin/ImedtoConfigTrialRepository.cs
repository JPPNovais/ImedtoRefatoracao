using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin;

public class ImedtoConfigTrialRepository : IImedtoConfigTrialRepository
{
    private readonly AppDbContext _db;

    public ImedtoConfigTrialRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ImedtoConfigTrial?> ObterAsync(CancellationToken ct = default)
        => await _db.ImedtoConfigsTrial.FindAsync([ImedtoConfigTrial.IdFixo], ct);

    public void Adicionar(ImedtoConfigTrial config)
        => _db.ImedtoConfigsTrial.Add(config);

    public void Atualizar(ImedtoConfigTrial config)
        => _db.ImedtoConfigsTrial.Update(config);
}
