using Imedto.Backend.Contracts.Admin.Dashboard.Queries;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Dashboard;

/// <summary>
/// Retorna 12 pontos de crescimento mensal de novos estabelecimentos.
/// Singleton: depende apenas de IDashboardAdminQueryRepository (singleton).
/// </summary>
public class ObterCrescimentoMensalDashboardAdminQueryHandler
{
    private readonly IDashboardAdminQueryRepository _repo;

    public ObterCrescimentoMensalDashboardAdminQueryHandler(IDashboardAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<CrescimentoMensalPontoDto>> Handle(
        ObterCrescimentoMensalDashboardQuery query,
        CancellationToken ct = default)
        => _repo.ObterCrescimentoMensalAsync(ct);
}
