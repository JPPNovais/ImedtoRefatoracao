using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Planos;
using Imedto.Backend.Contracts.Admin.Planos.Commands;
using Imedto.Backend.Contracts.Admin.Planos.Queries;
using Imedto.Backend.Contracts.Admin.Planos.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Gerenciamento do catalogo de planos Imedto (admin global).
/// F4: form de criacao/edicao passa features_json e limites_json.
/// </summary>
[ApiController]
[Route("api/admin/planos")]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminPlanosController : ControllerBase
{
    private readonly CriarPlanoAdminCommandHandler _criar;
    private readonly AtualizarPlanoAdminCommandHandler _atualizar;
    private readonly AtivarPlanoAdminCommandHandler _ativar;
    private readonly DesativarPlanoAdminCommandHandler _desativar;
    private readonly ListarPlanosAdminQueryHandler _listar;
    private readonly ObterPlanoAdminQueryHandler _obter;

    public AdminPlanosController(
        CriarPlanoAdminCommandHandler criar,
        AtualizarPlanoAdminCommandHandler atualizar,
        AtivarPlanoAdminCommandHandler ativar,
        DesativarPlanoAdminCommandHandler desativar,
        ListarPlanosAdminQueryHandler listar,
        ObterPlanoAdminQueryHandler obter)
    {
        _criar = criar;
        _atualizar = atualizar;
        _ativar = ativar;
        _desativar = desativar;
        _listar = listar;
        _obter = obter;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ListarPlanosAdminResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool? ativo,
        [FromQuery] string? busca,
        [FromQuery] int page = 1,
        [FromQuery] int size = 25,
        CancellationToken ct = default)
    {
        var result = await _listar.Handle(new ListarPlanosAdminQuery(ativo, busca, page, size), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlanoAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct = default)
    {
        var result = await _obter.Handle(new ObterPlanoAdminQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarPlanoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _criar.Handle(new CriarPlanoAdminCommand(
            request.Nome,
            request.DescricaoCurta,
            request.PrecoMensalCentavos,
            request.Gratuito,
            request.LimitesJson ?? "{}",
            request.FeaturesJson ?? "{}",
            request.Motivo,
            adminId), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarPlanoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarPlanoAdminCommand(
            id,
            request.Nome,
            request.DescricaoCurta,
            request.PrecoMensalCentavos,
            request.Gratuito,
            request.LimitesJson ?? "{}",
            request.FeaturesJson ?? "{}",
            request.Motivo,
            adminId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/ativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Ativar(Guid id, [FromBody] PlanoMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _ativar.Handle(new AtivarPlanoAdminCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/desativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desativar(Guid id, [FromBody] PlanoMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _desativar.Handle(new DesativarPlanoAdminCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    private Guid ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var id))
            throw new BusinessException("Sessão de administrador inválida.");
        return id;
    }
}

// Request bodies locais (não compartilhados, pois mapeiam para commands com AdminId injetado).
public record CriarPlanoRequest(
    string Nome,
    string? DescricaoCurta,
    int? PrecoMensalCentavos,
    bool Gratuito,
    string? LimitesJson,
    string? FeaturesJson,
    string Motivo);

public record AtualizarPlanoRequest(
    string Nome,
    string? DescricaoCurta,
    int? PrecoMensalCentavos,
    bool Gratuito,
    string? LimitesJson,
    string? FeaturesJson,
    string Motivo);

/// <summary>Renomeado de MotivoRequest para evitar conflito com o record do AdminAssinaturasController.</summary>
public record PlanoMotivoRequest(string Motivo);
