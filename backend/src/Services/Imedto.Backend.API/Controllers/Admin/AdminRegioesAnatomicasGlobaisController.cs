using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Regioes;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminRegioesAnatomicasGlobaisController : ControllerBase
{
    private readonly ListarArvoreRegioesAdminQueryHandler _listar;
    private readonly ObterRegiaoAdminQueryHandler _obter;
    private readonly CriarRegiaoAdminCommandHandler _criar;
    private readonly AtualizarRegiaoAdminCommandHandler _atualizar;
    private readonly InativarRegiaoAdminCommandHandler _inativar;
    private readonly ReativarRegiaoAdminCommandHandler _reativar;
    private readonly ExcluirRegiaoAdminCommandHandler _excluir;

    public AdminRegioesAnatomicasGlobaisController(
        ListarArvoreRegioesAdminQueryHandler listar,
        ObterRegiaoAdminQueryHandler obter,
        CriarRegiaoAdminCommandHandler criar,
        AtualizarRegiaoAdminCommandHandler atualizar,
        InativarRegiaoAdminCommandHandler inativar,
        ReativarRegiaoAdminCommandHandler reativar,
        ExcluirRegiaoAdminCommandHandler excluir)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _inativar = inativar;
        _reativar = reativar;
        _excluir = excluir;
    }

    [HttpGet("api/admin/catalogos/regioes-anatomicas")]
    public async Task<IActionResult> ListarArvore(
        [FromQuery] bool incluirInativas = false,
        CancellationToken ct = default)
    {
        var arvore = await _listar.Handle(incluirInativas, ct);
        return Ok(arvore);
    }

    [HttpGet("api/admin/catalogos/regioes-anatomicas/{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/regioes-anatomicas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarRegiaoAdminRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarRegiaoAdminCommand(
            request.Codigo,
            request.Nome,
            request.PaiCodigo,
            request.Nivel,
            request.Vista,
            request.TemplateTexto,
            request.Ordem,
            request.Lateralidade,
            request.Motivo,
            adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/regioes-anatomicas/{id:long}")]
    public async Task<IActionResult> Atualizar(
        long id,
        [FromBody] AtualizarRegiaoAdminRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarRegiaoAdminCommand(
            id, request.Nome, request.TemplateTexto, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/regioes-anatomicas/{id:long}/inativar")]
    public async Task<IActionResult> Inativar(long id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _inativar.Handle(new InativarRegiaoAdminCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/catalogos/regioes-anatomicas/{id:long}/reativar")]
    public async Task<IActionResult> Reativar(long id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarRegiaoAdminCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    [HttpDelete("api/admin/catalogos/regioes-anatomicas/{id:long}")]
    public async Task<IActionResult> Excluir(long id, [FromBody] AdminMotivoRequest request, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _excluir.Handle(new ExcluirRegiaoAdminCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public class CriarRegiaoAdminRequest
{
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string? PaiCodigo { get; init; }
    public short Nivel { get; init; }
    public string? Vista { get; init; }
    public string? TemplateTexto { get; init; }
    public short Ordem { get; init; }
    public bool Lateralidade { get; init; }
    public string Motivo { get; init; } = string.Empty;
}

public class AtualizarRegiaoAdminRequest
{
    public string Nome { get; init; } = string.Empty;
    public string? TemplateTexto { get; init; }
    public string Motivo { get; init; } = string.Empty;
}
