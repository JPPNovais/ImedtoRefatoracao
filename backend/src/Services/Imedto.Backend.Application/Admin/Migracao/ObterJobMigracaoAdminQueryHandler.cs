using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Migracao;

public sealed class ObterJobMigracaoAdminQueryHandler
{
    private readonly MigracaoAdminQueryRepository _repo;

    public ObterJobMigracaoAdminQueryHandler(MigracaoAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public async Task<MigracaoJobAdminDto?> Handle(
        ObterJobMigracaoAdminQuery query,
        CancellationToken ct = default)
    {
        return await _repo.ObterJobAsync(query.JobId, ct);
    }
}
