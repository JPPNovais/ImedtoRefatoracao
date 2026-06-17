using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.VariaveisPadraoSistema;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminVariaveisPoolGlobaisController : ControllerBase
{
    private readonly ListarVariaveisPadraoSistemaQueryHandler _listar;
    private readonly ObterVariavelPadraoSistemaQueryHandler _obter;
    private readonly CriarVariavelPadraoSistemaCommandHandler _criar;
    private readonly AtualizarVariavelPadraoSistemaCommandHandler _atualizar;
    private readonly InativarVariavelPadraoSistemaCommandHandler _inativar;
    private readonly ReativarVariavelPadraoSistemaCommandHandler _reativar;

    public AdminVariaveisPoolGlobaisController(
        ListarVariaveisPadraoSistemaQueryHandler listar,
        ObterVariavelPadraoSistemaQueryHandler obter,
        CriarVariavelPadraoSistemaCommandHandler criar,
        AtualizarVariavelPadraoSistemaCommandHandler atualizar,
        InativarVariavelPadraoSistemaCommandHandler inativar,
        ReativarVariavelPadraoSistemaCommandHandler reativar)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _inativar = inativar;
        _reativar = reativar;
    }

    [HttpGet("api/admin/catalogos/variaveis-pool")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] string? categoria = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken ct = default)
    {
        var (itens, total) = await _listar.Handle(incluirInativos, busca, categoria, pagina, tamanhoPagina, ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    [HttpGet("api/admin/catalogos/variaveis-pool/{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/variaveis-pool")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarVariavelPadraoSistemaRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarVariavelPadraoSistemaCommand(
            request.Nome, request.Tipo, request.Motivo, adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/variaveis-pool/{id:long}")]
    public async Task<IActionResult> Atualizar(
        long id,
        [FromBody] AtualizarVariavelPadraoSistemaRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarVariavelPadraoSistemaCommand(
            id, request.Nome, request.Tipo, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/variaveis-pool/{id:long}/inativar")]
    public async Task<IActionResult> Inativar(long id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _inativar.Handle(new InativarVariavelPadraoSistemaCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/variaveis-pool/{id:long}/reativar")]
    public async Task<IActionResult> Reativar(long id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarVariavelPadraoSistemaCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public class CriarVariavelPadraoSistemaRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}

public class AtualizarVariavelPadraoSistemaRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
}
