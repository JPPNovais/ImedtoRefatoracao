using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// CRUD de modelos de permissão padrão do sistema (escopo global, propagação cross-tenant).
/// Briefing 2026-06-04_001. Policy ImedtoAdmin — nunca acessível por JWT de tenant (CA7, CA8).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminModelosPermissaoGlobaisController : ControllerBase
{
    private readonly ListarModelosPermissaoPadraoSistemaQueryHandler _listar;
    private readonly ObterModeloPermissaoPadraoSistemaQueryHandler _obter;
    private readonly CriarModeloPermissaoPadraoSistemaCommandHandler _criar;
    private readonly AtualizarModeloPermissaoPadraoSistemaCommandHandler _atualizar;
    private readonly ExcluirModeloPermissaoPadraoSistemaCommandHandler _excluir;

    public AdminModelosPermissaoGlobaisController(
        ListarModelosPermissaoPadraoSistemaQueryHandler listar,
        ObterModeloPermissaoPadraoSistemaQueryHandler obter,
        CriarModeloPermissaoPadraoSistemaCommandHandler criar,
        AtualizarModeloPermissaoPadraoSistemaCommandHandler atualizar,
        ExcluirModeloPermissaoPadraoSistemaCommandHandler excluir)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _atualizar = atualizar;
        _excluir = excluir;
    }

    [HttpGet("api/admin/catalogos/permissoes")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        CancellationToken ct = default)
    {
        var (itens, total) = await _listar.Handle(busca, pagina, tamanhoPagina, ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    [HttpGet("api/admin/catalogos/permissoes/{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct = default)
    {
        var dto = await _obter.Handle(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("api/admin/catalogos/permissoes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarModeloPermissaoGlobalRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var id = await _criar.Handle(new CriarModeloPermissaoPadraoSistemaCommand(
            request.Nome,
            ParseTipoAcesso(request.TipoAcesso),
            request.Permissoes,
            null, // PermissoesExtras não são editadas pelo admin (R8)
            request.Icone,
            request.Cor,
            request.Descricao,
            adminId), ct);
        return CreatedAtAction(nameof(Obter), new { id }, new { id });
    }

    [HttpPut("api/admin/catalogos/permissoes/{id:long}")]
    public async Task<IActionResult> Atualizar(
        long id,
        [FromBody] AtualizarModeloPermissaoGlobalRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarModeloPermissaoPadraoSistemaCommand(
            id,
            request.Nome,
            ParseTipoAcesso(request.TipoAcesso),
            request.Permissoes,
            request.Icone,
            request.Cor,
            request.Descricao,
            adminId), ct);
        return NoContent();
    }

    [HttpDelete("api/admin/catalogos/permissoes/{id:long}")]
    public async Task<IActionResult> Excluir(long id, CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _excluir.Handle(new ExcluirModeloPermissaoPadraoSistemaCommand(id, adminId), ct);
        return NoContent();
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static TipoAcessoModelo ParseTipoAcesso(string tipoAcesso)
    {
        if (!Enum.TryParse<TipoAcessoModelo>(tipoAcesso, ignoreCase: true, out var result))
            throw new BusinessException($"Tipo de acesso inválido: {tipoAcesso}.");
        return result;
    }
}

public class CriarModeloPermissaoGlobalRequest
{
    public string Nome { get; init; } = string.Empty;
    public string TipoAcesso { get; init; } = "Profissional";
    public IReadOnlyList<string>? Permissoes { get; init; }
    public string? Icone { get; init; }
    public string? Cor { get; init; }
    public string? Descricao { get; init; }
}

public class AtualizarModeloPermissaoGlobalRequest
{
    public string Nome { get; init; } = string.Empty;
    public string TipoAcesso { get; init; } = "Profissional";
    public IReadOnlyList<string>? Permissoes { get; init; }
    public string? Icone { get; init; }
    public string? Cor { get; init; }
    public string? Descricao { get; init; }
}
