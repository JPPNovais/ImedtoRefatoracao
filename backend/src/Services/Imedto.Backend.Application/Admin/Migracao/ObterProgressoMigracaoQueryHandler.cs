using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Migracao;

public sealed class ObterProgressoMigracaoQueryHandler
{
    private readonly MigracaoAdminQueryRepository _repo;

    public ObterProgressoMigracaoQueryHandler(MigracaoAdminQueryRepository repo) => _repo = repo;

    public async Task<ProgressoMigracaoResult> Handle(long jobId, CancellationToken ct = default)
        => await _repo.ObterProgressoAsync(jobId, ct);
}
