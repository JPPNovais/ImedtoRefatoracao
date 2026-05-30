using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Estabelecimentos.Commands;
using Imedto.Backend.Application.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Endpoints admin para listagem, detalhe, reveal CPF e reset de estabelecimentos.
/// Todos protegidos pela policy <c>ImedtoAdmin</c> (claim imedto_admin=true + sem force_reset).
/// LGPD: CPF mascarado por padrão; reveal via endpoint dedicado com motivo obrigatório + audit.
/// Sem PII de paciente em nenhum endpoint (CA20).
/// </summary>
[ApiController]
[Authorize(Policy = "ImedtoAdmin")]
[Route("api/admin/estabelecimentos")]
[Produces("application/json")]
public class AdminEstabelecimentosController : ControllerBase
{
    private readonly ListarEstabelecimentosAdminQueryHandler _listarHandler;
    private readonly ObterEstabelecimentoAdminQueryHandler _obterHandler;
    private readonly RevelarCpfDonoQueryHandler _revelaCpfHandler;
    private readonly ResetTenantCommandHandler _resetHandler;

    public AdminEstabelecimentosController(
        ListarEstabelecimentosAdminQueryHandler listarHandler,
        ObterEstabelecimentoAdminQueryHandler obterHandler,
        RevelarCpfDonoQueryHandler revelaCpfHandler,
        ResetTenantCommandHandler resetHandler)
    {
        _listarHandler = listarHandler;
        _obterHandler = obterHandler;
        _revelaCpfHandler = revelaCpfHandler;
        _resetHandler = resetHandler;
    }

    /// <summary>
    /// Lista paginada de estabelecimentos (CA24–CA26).
    /// CPF do dono mascarado. Busca via índice GIN pg_trgm.
    /// </summary>
    /// <param name="busca">Filtro por nome fantasia (ILIKE, usa GIN).</param>
    /// <param name="status">Filtro opcional por status do estabelecimento.</param>
    /// <param name="page">Página (base 1, default 1).</param>
    /// <param name="size">Tamanho da página (max 100, default 25).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaEstabelecimentosAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 25)
    {
        var resultado = await _listarHandler.Handle(new ListarEstabelecimentosAdminQuery
        {
            Busca = busca,
            Status = status,
            Pagina = page,
            TamanhoPagina = size,
        });

        return Ok(resultado);
    }

    /// <summary>
    /// Detalhe de um estabelecimento. CPF mascarado. Sem PII de paciente (CA20).
    /// Registra audit de leitura de detalhe (CA13/CA15).
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(EstabelecimentoAdminDetalheDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        var dto = await _obterHandler.Handle(new ObterEstabelecimentoAdminQuery
        {
            EstabelecimentoId = id,
            AdminId = adminId.Value,
        });

        return Ok(dto);
    }

    /// <summary>
    /// Revela CPF completo do dono (CA17–CA19).
    /// Motivo obrigatório (mín. 10 chars). Gera audit <c>REVELAR_CPF_DONO</c>.
    /// </summary>
    [HttpPost("{id:long}/revelar-cpf-dono")]
    [ProducesResponseType(typeof(CpfDonoReveladoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RevelarCpfDono(
        long id,
        [FromBody] RevelarCpfDonoRequest request,
        CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        var resultado = await _revelaCpfHandler.Handle(new RevelarCpfDonoQuery
        {
            EstabelecimentoId = id,
            AdminId = adminId.Value,
            Motivo = request.Motivo,
        });

        return Ok(resultado);
    }

    /// <summary>
    /// Reseta dados do tenant (CA32–CA36).
    /// Confirmação dupla: nome fantasia exato + motivo (mín. 10 chars).
    /// Gera audit <c>RESETAR_TENANT</c>. Reusa <see cref="IAdminResetService"/>.
    /// </summary>
    [HttpPost("{id:long}/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetTenant(
        long id,
        [FromBody] ResetTenantRequest request,
        CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        await _resetHandler.Handle(new ResetTenantCommand
        {
            EstabelecimentoId = id,
            AdminId = adminId.Value,
            ConfirmarNomeFantasia = request.ConfirmarNomeFantasia,
            Motivo = request.Motivo,
        });

        return NoContent();
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

/// <param name="Motivo">Motivo para audit (mín. 10 chars).</param>
public record RevelarCpfDonoRequest(string Motivo);

/// <param name="Motivo">Motivo do reset (mín. 10 chars).</param>
/// <param name="ConfirmarNomeFantasia">Nome fantasia digitado pelo admin para confirmação dupla.</param>
public record ResetTenantRequest(string Motivo, string ConfirmarNomeFantasia);
