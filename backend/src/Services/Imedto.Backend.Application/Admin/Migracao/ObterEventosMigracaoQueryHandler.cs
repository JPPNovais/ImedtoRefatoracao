using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Migracao;

public sealed class ObterEventosMigracaoQueryHandler
{
    private readonly MigracaoAdminQueryRepository _repo;

    public ObterEventosMigracaoQueryHandler(MigracaoAdminQueryRepository repo) => _repo = repo;

    public async Task<List<MigracaoJobEventoDto>> Handle(long jobId, CancellationToken ct = default)
        => await _repo.ListarEventosAsync(jobId, ct);
}
