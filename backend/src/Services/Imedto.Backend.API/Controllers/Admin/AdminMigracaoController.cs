using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Endpoints admin para gestão da Central de Migração.
/// Todos protegidos pela policy ImedtoAdmin.
/// LGPD: sem PII de paciente em nenhum endpoint (apenas metadados de job e entidades).
/// </summary>
[ApiController]
[Authorize(Policy = "ImedtoAdmin")]
[Route("api/admin/migracao")]
[Produces("application/json")]
public class AdminMigracaoController : ControllerBase
{
    private readonly ListarJobsMigracaoAdminQueryHandler _listarHandler;
    private readonly ObterJobMigracaoAdminQueryHandler   _obterHandler;
    private readonly SalvarMapaRevisadoCommandHandler    _salvarMapaHandler;
    private readonly SalvarTemplateDeOrigemCommandHandler _salvarTemplateHandler;
    private readonly PreviewOnda1QueryHandler            _previewHandler;
    private readonly DisparaMigracaoCommandHandler       _disparaHandler;
    private readonly RelatorioMigracaoQueryHandler       _relatorioHandler;

    public AdminMigracaoController(
        ListarJobsMigracaoAdminQueryHandler listarHandler,
        ObterJobMigracaoAdminQueryHandler   obterHandler,
        SalvarMapaRevisadoCommandHandler    salvarMapaHandler,
        SalvarTemplateDeOrigemCommandHandler salvarTemplateHandler,
        PreviewOnda1QueryHandler            previewHandler,
        DisparaMigracaoCommandHandler       disparaHandler,
        RelatorioMigracaoQueryHandler       relatorioHandler)
    {
        _listarHandler        = listarHandler;
        _obterHandler         = obterHandler;
        _salvarMapaHandler    = salvarMapaHandler;
        _salvarTemplateHandler = salvarTemplateHandler;
        _previewHandler       = previewHandler;
        _disparaHandler       = disparaHandler;
        _relatorioHandler     = relatorioHandler;
    }

    /// <summary>Lista jobs de migração (filtros opcionais: estabelecimento, status).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListarJobsMigracaoAdminResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] long? estabelecimentoId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 25,
        CancellationToken ct = default)
    {
        var resultado = await _listarHandler.Handle(new ListarJobsMigracaoAdminQuery
        {
            EstabelecimentoId = estabelecimentoId,
            Status  = status,
            Pagina  = page,
            Tamanho = size,
        }, ct);

        return Ok(resultado);
    }

    /// <summary>Detalhe de um job + mapas por entidade.</summary>
    [HttpGet("{jobId:long}")]
    [ProducesResponseType(typeof(MigracaoJobAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long jobId, CancellationToken ct)
    {
        var dto = await _obterHandler.Handle(new ObterJobMigracaoAdminQuery { JobId = jobId }, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Salva mapa revisado para uma entidade específica.</summary>
    [HttpPut("{jobId:long}/mapas/{entidade}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SalvarMapa(
        long jobId,
        string entidade,
        [FromBody] SalvarMapaRevisadoBody body,
        CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        await _salvarMapaHandler.Handle(new SalvarMapaRevisadoCommand
        {
            JobId             = jobId,
            Entidade          = entidade,
            DePara            = body.DePara,
            RevisadoPorUsuarioId = adminId.Value,
        }, ct);

        return NoContent();
    }

    /// <summary>Salva os mapas revisados como template reutilizável (CA18).</summary>
    [HttpPost("{jobId:long}/template")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SalvarTemplate(
        long jobId,
        [FromBody] SalvarTemplateBody body,
        CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        await _salvarTemplateHandler.Handle(new SalvarTemplateDeOrigemCommand
        {
            JobId             = jobId,
            NomeTemplate      = body.NomeTemplate,
            RevisadoPorUsuarioId = adminId.Value,
        }, ct);

        return NoContent();
    }

    /// <summary>Gera preview — muda job para preview_pronto.</summary>
    [HttpPut("{jobId:long}/preview-pronto")]
    [ProducesResponseType(typeof(PreviewMigracaoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GerarPreview(long jobId, CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();
        var resultado = await _previewHandler.Handle(jobId, adminId.Value, ct);
        return Ok(resultado);
    }

    /// <summary>Dispara carga assíncrona — CA22: retorna 202 imediatamente.</summary>
    [HttpPost("{jobId:long}/disparar")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Disparar(long jobId, CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();
        await _disparaHandler.Handle(new DisparaMigracaoCommand { JobId = jobId, AdminId = adminId.Value }, ct);
        return Accepted();
    }

    /// <summary>Relatório final da carga.</summary>
    [HttpGet("{jobId:long}/relatorio")]
    [ProducesResponseType(typeof(RelatorioMigracaoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Relatorio(long jobId, CancellationToken ct)
    {
        var resultado = await _relatorioHandler.Handle(jobId, ct);
        return Ok(resultado);
    }

    private Guid? ObterAdminId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("admin_id");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}

// Body DTOs inline (request bodies simples — sem namespace dedicado).
public sealed class SalvarMapaRevisadoBody
{
    public Dictionary<string, string> DePara { get; init; } = [];
}

public sealed class SalvarTemplateBody
{
    public string NomeTemplate { get; init; } = string.Empty;
}
