using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.CategoriasFinanceiras;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// CRUD do catálogo global de categorias financeiras padrão (escopo plataforma, propagação cross-tenant).
/// Briefing 2026-06-22_003 — M3. Policy ImedtoAdmin — nunca acessível por JWT de tenant (CA7).
/// SEM endpoint de rename (R5 — nome é imutável).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminCategoriasFinanceirasGlobaisController : ControllerBase
{
    private readonly ListarCategoriasFinanceirasPadraoSistemaQueryHandler _listar;
    private readonly CriarCategoriaFinanceiraPadraoSistemaCommandHandler _criar;
    private readonly InativarCategoriaFinanceiraPadraoSistemaCommandHandler _inativar;
    private readonly ReativarCategoriaFinanceiraPadraoSistemaCommandHandler _reativar;

    public AdminCategoriasFinanceirasGlobaisController(
        ListarCategoriasFinanceirasPadraoSistemaQueryHandler listar,
        CriarCategoriaFinanceiraPadraoSistemaCommandHandler criar,
        InativarCategoriaFinanceiraPadraoSistemaCommandHandler inativar,
        ReativarCategoriaFinanceiraPadraoSistemaCommandHandler reativar)
    {
        _listar = listar;
        _criar = criar;
        _inativar = inativar;
        _reativar = reativar;
    }

    /// <summary>Lista o catálogo global com filtro opcional por tipo e ativo.</summary>
    [HttpGet("api/admin/catalogos/categorias-financeiras")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? tipo = null,
        [FromQuery] bool? ativas = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 50,
        CancellationToken ct = default)
    {
        var (itens, total) = await _listar.Handle(tipo, ativas, pagina, tamanhoPagina, ct);
        return Ok(new { itens, total, pagina, tamanhoPagina });
    }

    /// <summary>Cria nova categoria padrão global e propaga a todos os estabelecimentos.</summary>
    [HttpPost("api/admin/catalogos/categorias-financeiras")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarCategoriaFinanceiraGlobalRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        var tipo = ParseTipo(request.Tipo);
        var id = await _criar.Handle(new CriarCategoriaFinanceiraPadraoSistemaCommand(
            request.Nome,
            tipo,
            adminId), ct);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>Inativa a categoria padrão global e reflete nas cópias dos estabelecimentos (R4).</summary>
    [HttpPost("api/admin/catalogos/categorias-financeiras/{id:long}/inativar")]
    public async Task<IActionResult> Inativar(long id, CancellationToken ct = default)
    {
        await _inativar.Handle(new InativarCategoriaFinanceiraPadraoSistemaCommand(id, ObterAdminId()), ct);
        return NoContent();
    }

    /// <summary>Reativa a categoria padrão global e reflete nas cópias dos estabelecimentos (R4.1).</summary>
    [HttpPost("api/admin/catalogos/categorias-financeiras/{id:long}/reativar")]
    public async Task<IActionResult> Reativar(long id, CancellationToken ct = default)
    {
        await _reativar.Handle(new ReativarCategoriaFinanceiraPadraoSistemaCommand(id, ObterAdminId()), ct);
        return NoContent();
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static TipoCategoria ParseTipo(string tipo)
    {
        if (!Enum.TryParse<TipoCategoria>(tipo, ignoreCase: true, out var result))
            throw new BusinessException($"Tipo inválido: {tipo}. Use 'Receita' ou 'Despesa'.");
        return result;
    }
}

public class CriarCategoriaFinanceiraGlobalRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = "Receita";
}
