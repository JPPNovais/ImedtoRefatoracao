using Imedto.Backend.Contracts.Admin.Dashboard.Queries;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Dashboard;

/// <summary>
/// Retorna KPIs do dashboard admin: estabelecimentos, admins, trials, assinaturas.
/// Singleton: depende apenas de IDashboardAdminQueryRepository (singleton).
/// Leitura não gera audit (Wave 1 CA16 / W6-CA22).
/// </summary>
public class ObterKpisDashboardAdminQueryHandler
{
    private readonly IDashboardAdminQueryRepository _repo;

    public ObterKpisDashboardAdminQueryHandler(IDashboardAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<KpisDashboardDto> Handle(ObterKpisDashboardQuery query, CancellationToken ct = default)
        => _repo.ObterKpisAsync(ct);
}
