using Imedto.Backend.Contracts.Admin.Dashboard.Queries;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;

namespace Imedto.Backend.Application.Admin.Dashboard;

/// <summary>
/// Feed paginado do audit log com filtros por ação, admin e período.
/// Singleton: depende apenas de IDashboardAdminQueryRepository (singleton).
/// </summary>
public class ListarAuditLogDashboardAdminQueryHandler
{
    private readonly IDashboardAdminQueryRepository _repo;

    public ListarAuditLogDashboardAdminQueryHandler(IDashboardAdminQueryRepository repo)
    {
        _repo = repo;
    }

    public async Task<AuditLogPaginadoDto> Handle(ListarAuditLogDashboardQuery query, CancellationToken ct = default)
    {
        var tamanhoPagina = query.TamanhoPagina is > 0 and <= 100 ? query.TamanhoPagina : 20;
        var pagina = query.Pagina >= 1 ? query.Pagina : 1;
        var periodo = string.IsNullOrWhiteSpace(query.Periodo) ? "7d" : query.Periodo;

        return await _repo.ListarAuditLogAsync(
            query.Acao,
            query.AdminId,
            periodo,
            pagina,
            tamanhoPagina,
            ct);
    }
}
