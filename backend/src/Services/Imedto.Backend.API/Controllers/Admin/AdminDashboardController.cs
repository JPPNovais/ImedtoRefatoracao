using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Dashboard;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Endpoints do dashboard operacional do admin global.
/// Toda rota exige policy ImedtoAdmin.
/// Leitura não gera audit (Wave 1 CA16 / W6-CA22).
/// </summary>
[ApiController]
[Authorize(Policy = "ImedtoAdmin")]
[Route("api/admin/dashboard")]
[Produces("application/json")]
public class AdminDashboardController : ControllerBase
{
    private readonly ObterKpisDashboardAdminQueryHandler _kpis;
    private readonly ObterCrescimentoMensalDashboardAdminQueryHandler _crescimento;
    private readonly ObterAlertasDashboardAdminQueryHandler _alertas;
    private readonly ListarAuditLogDashboardAdminQueryHandler _auditLog;

    public AdminDashboardController(
        ObterKpisDashboardAdminQueryHandler kpis,
        ObterCrescimentoMensalDashboardAdminQueryHandler crescimento,
        ObterAlertasDashboardAdminQueryHandler alertas,
        ListarAuditLogDashboardAdminQueryHandler auditLog)
    {
        _kpis = kpis;
        _crescimento = crescimento;
        _alertas = alertas;
        _auditLog = auditLog;
    }

    /// <summary>KPIs do painel: estabelecimentos, admins, trials, assinaturas.</summary>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(KpisDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterKpis(CancellationToken ct)
    {
        var resultado = await _kpis.Handle(new ObterKpisDashboardQuery(), ct);
        return Ok(resultado);
    }

    /// <summary>12 pontos de crescimento mensal de novos estabelecimentos.</summary>
    [HttpGet("crescimento-mensal")]
    [ProducesResponseType(typeof(IReadOnlyList<CrescimentoMensalPontoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterCrescimentoMensal(CancellationToken ct)
    {
        var resultado = await _crescimento.Handle(new ObterCrescimentoMensalDashboardQuery(), ct);
        return Ok(resultado);
    }

    /// <summary>Alertas acionáveis: trials expirando em 7 dias e estabelecimentos sem assinatura.</summary>
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(AlertasDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterAlertas(CancellationToken ct)
    {
        var resultado = await _alertas.Handle(new ObterAlertasDashboardQuery(), ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Feed paginado de audit log com filtros opcionais.
    /// Período: "hoje" | "7d" | "30d" | "90d" | "todos". Default "7d".
    /// </summary>
    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(AuditLogPaginadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAuditLog(
        [FromQuery] string? acao,
        [FromQuery] Guid? adminId,
        [FromQuery] string periodo = "7d",
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var query = new ListarAuditLogDashboardQuery
        {
            Acao = acao,
            AdminId = adminId,
            Periodo = periodo,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
        var resultado = await _auditLog.Handle(query, ct);
        return Ok(resultado);
    }
}
