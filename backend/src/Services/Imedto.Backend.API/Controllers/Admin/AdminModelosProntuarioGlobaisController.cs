using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Catalogos.Modelos;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Commands;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminModelosProntuarioGlobaisController : ControllerBase
{
    private readonly ListarModelosGlobaisQueryHandler _listar;
    private readonly ObterModeloGlobalQueryHandler _obter;
    private readonly CriarModeloGlobalCommandHandler _criar;
    private readonly AtualizarModeloGlobalCommandHandler _atualizar;
    private readonly DesativarModeloGlobalCommandHandler _desativar;
    private readonly ReativarModeloGlobalCommandHandler _reativar;

    public AdminModelosProntuarioGlobaisController(
        ListarModelosGlobaisQueryHandler listar,
        ObterModeloGlobalQueryHandler obter,
        CriarModeloGlobalCommandHandler criar,
        AtualizarModeloGlobalCommandHandler atualizar,
        DesativarModeloGlobalCommandHandler desativar,
        ReativarModeloGlobalCommandHandler reativar)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _desativar = desativar;
        _reativar = reativar;
    }

    [HttpGet("api/admin/catalogos/modelos-prontuario")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var (itens, total) = await _listar.Handle(
            new ListarModelosGlobaisQuery(incluirInativos, busca, pagina, tamanhoPagina), ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    [HttpGet("api/admin/catalogos/modelos-prontuario/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(new ObterModeloGlobalQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/modelos-prontuario")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarModeloGlobalRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarModeloGlobalCommand(
            request.Nome, request.Descricao, request.ConteudoJson, request.Motivo, adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/modelos-prontuario/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] AtualizarModeloGlobalRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarModeloGlobalCommand(
            id, request.Nome, request.Descricao, request.ConteudoJson, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/modelos-prontuario/{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(
        Guid id,
        [FromBody] AdminMotivoRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _desativar.Handle(new DesativarModeloGlobalCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/modelos-prontuario/{id:guid}/reativar")]
    public async Task<IActionResult> Reativar(
        Guid id,
        [FromBody] AdminMotivoRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarModeloGlobalCommand(id, request.Motivo, adminId), ct);
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

public record CriarModeloGlobalRequest(string Nome, string? Descricao, string ConteudoJson, string Motivo);
public record AtualizarModeloGlobalRequest(string Nome, string? Descricao, string ConteudoJson, string Motivo);
public record AdminMotivoRequest(string Motivo);
