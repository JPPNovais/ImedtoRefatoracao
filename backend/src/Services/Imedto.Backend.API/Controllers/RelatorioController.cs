using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Relatorios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize]
[RequiresEstabelecimento]
public class RelatorioController : ControllerBase
{
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public RelatorioController(IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _query = query;
        _tenant = tenant;
    }

    [HttpGet("faturamento")]
    public async Task<ActionResult<IEnumerable<FaturamentoCategoriaDto>>> Faturamento(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim)
    {
        var result = await _query.Query<RelatorioFaturamentoQuery, IEnumerable<FaturamentoCategoriaDto>>(
            new RelatorioFaturamentoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }

    [HttpGet("agendamentos")]
    public async Task<ActionResult<RelatorioAgendamentosDto>> Agendamentos(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim)
    {
        var result = await _query.Query<RelatorioAgendamentosQuery, RelatorioAgendamentosDto>(
            new RelatorioAgendamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }
}
