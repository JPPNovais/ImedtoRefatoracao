using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Catalogos.Variaveis;
using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Commands;
using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminVariaveisPoolGlobaisController : ControllerBase
{
    private readonly ListarVariaveisGlobaisQueryHandler _listar;
    private readonly ObterVariavelGlobalQueryHandler _obter;
    private readonly CriarVariavelGlobalCommandHandler _criar;
    private readonly AtualizarVariavelGlobalCommandHandler _atualizar;
    private readonly DesativarVariavelGlobalCommandHandler _desativar;
    private readonly ReativarVariavelGlobalCommandHandler _reativar;

    public AdminVariaveisPoolGlobaisController(
        ListarVariaveisGlobaisQueryHandler listar,
        ObterVariavelGlobalQueryHandler obter,
        CriarVariavelGlobalCommandHandler criar,
        AtualizarVariavelGlobalCommandHandler atualizar,
        DesativarVariavelGlobalCommandHandler desativar,
        ReativarVariavelGlobalCommandHandler reativar)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _desativar = desativar;
        _reativar = reativar;
    }

    [HttpGet("api/admin/catalogos/variaveis-pool")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] string? tipo = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var (itens, total) = await _listar.Handle(
            new ListarVariaveisGlobaisQuery(incluirInativos, busca, tipo, pagina, tamanhoPagina), ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    [HttpGet("api/admin/catalogos/variaveis-pool/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(new ObterVariavelGlobalQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/variaveis-pool")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarVariavelGlobalRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarVariavelGlobalCommand(
            request.Nome, request.Tipo, request.ValoresJson, request.Descricao, request.Motivo, adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/variaveis-pool/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarVariavelGlobalRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarVariavelGlobalCommand(
            id, request.Nome, request.Tipo, request.ValoresJson, request.Descricao, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/variaveis-pool/{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(Guid id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _desativar.Handle(new DesativarVariavelGlobalCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/variaveis-pool/{id:guid}/reativar")]
    public async Task<IActionResult> Reativar(Guid id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarVariavelGlobalCommand(id, request.Motivo, adminId), ct);
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

public record CriarVariavelGlobalRequest(string Nome, string Tipo, string? ValoresJson, string? Descricao, string Motivo);
public record AtualizarVariavelGlobalRequest(string Nome, string Tipo, string? ValoresJson, string? Descricao, string Motivo);
