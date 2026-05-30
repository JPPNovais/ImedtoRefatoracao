using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Catalogos.Regioes;
using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Commands;
using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminRegioesAnatomicasGlobaisController : ControllerBase
{
    private readonly ListarRegioesGlobaisQueryHandler _listar;
    private readonly ObterRegiaoGlobalQueryHandler _obter;
    private readonly CriarRegiaoGlobalCommandHandler _criar;
    private readonly AtualizarRegiaoGlobalCommandHandler _atualizar;
    private readonly DesativarRegiaoGlobalCommandHandler _desativar;
    private readonly ReativarRegiaoGlobalCommandHandler _reativar;

    public AdminRegioesAnatomicasGlobaisController(
        ListarRegioesGlobaisQueryHandler listar,
        ObterRegiaoGlobalQueryHandler obter,
        CriarRegiaoGlobalCommandHandler criar,
        AtualizarRegiaoGlobalCommandHandler atualizar,
        DesativarRegiaoGlobalCommandHandler desativar,
        ReativarRegiaoGlobalCommandHandler reativar)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _desativar = desativar;
        _reativar = reativar;
    }

    [HttpGet("api/admin/catalogos/regioes-anatomicas")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] string? sistemaCorporal = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var (itens, total) = await _listar.Handle(
            new ListarRegioesGlobaisQuery(incluirInativos, busca, sistemaCorporal, pagina, tamanhoPagina), ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    [HttpGet("api/admin/catalogos/regioes-anatomicas/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(new ObterRegiaoGlobalQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/regioes-anatomicas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarRegiaoGlobalRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarRegiaoGlobalCommand(
            request.Nome, request.Sinonimos, request.SistemaCorporal, request.Motivo, adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/regioes-anatomicas/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarRegiaoGlobalRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarRegiaoGlobalCommand(
            id, request.Nome, request.Sinonimos, request.SistemaCorporal, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/regioes-anatomicas/{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(Guid id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _desativar.Handle(new DesativarRegiaoGlobalCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/regioes-anatomicas/{id:guid}/reativar")]
    public async Task<IActionResult> Reativar(Guid id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarRegiaoGlobalCommand(id, request.Motivo, adminId), ct);
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

public record CriarRegiaoGlobalRequest(string Nome, string[]? Sinonimos, string? SistemaCorporal, string Motivo);
public record AtualizarRegiaoGlobalRequest(string Nome, string[]? Sinonimos, string? SistemaCorporal, string Motivo);
