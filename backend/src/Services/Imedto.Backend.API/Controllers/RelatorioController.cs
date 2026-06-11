using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Relatorios;
using Imedto.Backend.Contracts.Relatorios.Queries;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAcao("relatorios")]
public class RelatorioController : ControllerBase
{
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public RelatorioController(IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _query = query;
        _tenant = tenant;
    }

    // -------------------------------------------------------------------------
    // Endpoints legados (mantidos por compat — front migra gradualmente).
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Endpoints consolidados (item 4.1 — substituem os 9 RPCs SQL legados).
    // FeatureGate apenas em pessoas/orcamentos (planos premium); financeiro e
    // operacional ficam livres (planos básicos).
    // -------------------------------------------------------------------------

    [HttpGet("financeiro")]
    public async Task<ActionResult<RelatorioFinanceiroDto>> Financeiro(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        [FromQuery] string agruparPor = "dia",
        [FromQuery] bool incluirPorPaciente = false)
    {
        var result = await _query.Query<RelatorioFinanceiroQuery, RelatorioFinanceiroDto>(
            new RelatorioFinanceiroQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                AgruparPor = agruparPor,
                IncluirPorPaciente = incluirPorPaciente
            });
        return Ok(result);
    }

    [HttpGet("operacional")]
    public async Task<ActionResult<RelatorioOperacionalDto>> Operacional(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        [FromQuery] string tipo = "dashboard")
    {
        var result = await _query.Query<RelatorioOperacionalQuery, RelatorioOperacionalDto>(
            new RelatorioOperacionalQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Tipo = tipo
            });
        return Ok(result);
    }

    [HttpGet("pessoas")]
    [FeatureGate(Features.RelatoriosAvancados)]
    public async Task<ActionResult<RelatorioPessoasDto>> Pessoas(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        [FromQuery] string tipo = "pacientes")
    {
        var result = await _query.Query<RelatorioPessoasQuery, RelatorioPessoasDto>(
            new RelatorioPessoasQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Tipo = tipo
            });
        return Ok(result);
    }

    [HttpGet("orcamentos")]
    [FeatureGate(Features.RelatoriosAvancados)]
    public async Task<ActionResult<RelatorioOrcamentosDto>> Orcamentos(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim)
    {
        var result = await _query.Query<RelatorioOrcamentosQuery, RelatorioOrcamentosDto>(
            new RelatorioOrcamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }
}
