using Imedto.Backend.Contracts.Dashboard;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Dashboard;

public class DashboardQueryHandlers : IRequestHandler<DashboardQuery, DashboardDto>
{
    private readonly DashboardQueryRepository _repo;

    public DashboardQueryHandlers(DashboardQueryRepository repo) => _repo = repo;

    public Task<DashboardDto> Handle(DashboardQuery query)
        => _repo.ObterDashboard(query.EstabelecimentoId);
}
