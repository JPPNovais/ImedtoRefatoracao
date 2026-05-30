using Imedto.Backend.Contracts.Admin.Dashboard.Queries;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Dashboard;

/// <summary>
/// Retorna alertas acionáveis: trials expirando em 7 dias e estabelecimentos sem assinatura.
/// Singleton: depende apenas de IDashboardAdminQueryRepository (singleton).
/// </summary>
public class ObterAlertasDashboardAdminQueryHandler
{
    private readonly IDashboardAdminQueryRepository _repo;

    public ObterAlertasDashboardAdminQueryHandler(IDashboardAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public Task<AlertasDashboardDto> Handle(ObterAlertasDashboardQuery query, CancellationToken ct = default)
        => _repo.ObterAlertasAsync(ct);
}
