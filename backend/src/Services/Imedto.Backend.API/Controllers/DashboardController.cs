using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Dashboard;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAcao("agenda")]
public class DashboardController : ControllerBase
{
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public DashboardController(IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _query = query;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obter()
    {
        var result = await _query.Query<DashboardQuery, DashboardDto>(
            new DashboardQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(result);
    }
}
