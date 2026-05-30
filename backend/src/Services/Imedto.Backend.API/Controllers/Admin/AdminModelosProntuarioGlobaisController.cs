using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.ModelosPadraoSistema;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminModelosProntuarioGlobaisController : ControllerBase
{
    private readonly ListarModelosPadraoSistemaQueryHandler _listar;
    private readonly ObterModeloPadraoSistemaQueryHandler _obter;
    private readonly CriarModeloPadraoSistemaCommandHandler _criar;
    private readonly AtualizarModeloPadraoSistemaCommandHandler _atualizar;
    private readonly InativarModeloPadraoSistemaCommandHandler _inativar;
    private readonly ReativarModeloPadraoSistemaCommandHandler _reativar;

    public AdminModelosProntuarioGlobaisController(
        ListarModelosPadraoSistemaQueryHandler listar,
        ObterModeloPadraoSistemaQueryHandler obter,
        CriarModeloPadraoSistemaCommandHandler criar,
        AtualizarModeloPadraoSistemaCommandHandler atualizar,
        InativarModeloPadraoSistemaCommandHandler inativar,
        ReativarModeloPadraoSistemaCommandHandler reativar)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _inativar = inativar;
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
        var (itens, total) = await _listar.Handle(incluirInativos, busca, pagina, tamanhoPagina, ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    [HttpGet("api/admin/catalogos/modelos-prontuario/{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/modelos-prontuario")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarModeloPadraoSistemaRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarModeloPadraoSistemaCommand(
            request.Nome, request.Descricao, request.EstruturaJson, request.Motivo, adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/modelos-prontuario/{id:long}")]
    public async Task<IActionResult> Atualizar(
        long id,
        [FromBody] AtualizarModeloPadraoSistemaRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarModeloPadraoSistemaCommand(
            id, request.Nome, request.Descricao, request.EstruturaJson, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/modelos-prontuario/{id:long}/inativar")]
    public async Task<IActionResult> Inativar(
        long id,
        [FromBody] AdminMotivoRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _inativar.Handle(new InativarModeloPadraoSistemaCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/modelos-prontuario/{id:long}/reativar")]
    public async Task<IActionResult> Reativar(
        long id,
        [FromBody] AdminMotivoRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarModeloPadraoSistemaCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public class CriarModeloPadraoSistemaRequest
{
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public string EstruturaJson { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}

public class AtualizarModeloPadraoSistemaRequest
{
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public string EstruturaJson { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}

public class AdminMotivoRequest
{
    public string Motivo { get; init; } = string.Empty;
}
