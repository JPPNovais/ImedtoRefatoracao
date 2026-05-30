using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Admins.Commands;
using Imedto.Backend.Application.Admin.Admins.Queries;
using Imedto.Backend.Contracts.Admin.Admins.Commands;
using Imedto.Backend.Contracts.Admin.Admins.Queries;
using Imedto.Backend.Contracts.Admin.Admins.Queries.Results;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// CRUD de administradores globais.
/// Toda rota exige policy ImedtoAdmin (claim imedto_admin = "true").
/// </summary>
[ApiController]
[Authorize(Policy = "ImedtoAdmin")]
[Route("api/admin/admins")]
[Produces("application/json")]
public class AdminAdminsController : ControllerBase
{
    private readonly ListarAdminsQueryHandler _listar;
    private readonly ObterAdminQueryHandler _obter;
    private readonly CriarAdminCommandHandler _criar;
    private readonly DesativarAdminCommandHandler _desativar;
    private readonly ReativarAdminCommandHandler _reativar;
    private readonly ResetSenhaAdminCommandHandler _resetSenha;

    public AdminAdminsController(
        ListarAdminsQueryHandler listar,
        ObterAdminQueryHandler obter,
        CriarAdminCommandHandler criar,
        DesativarAdminCommandHandler desativar,
        ReativarAdminCommandHandler reativar,
        ResetSenhaAdminCommandHandler resetSenha)
    {
        _listar = listar;
        _obter = obter;
        _criar = criar;
        _desativar = desativar;
        _reativar = reativar;
        _resetSenha = resetSenha;
    }

    /// <summary>Lista admins com paginação e busca por nome/e-mail (case-insensitive).</summary>
    /// <response code="200">Lista paginada de admins.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ListarAdminsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string busca,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 25,
        CancellationToken ct = default)
    {
        var resultado = await _listar.Handle(new ListarAdminsQuery(busca, pagina, tamanho));
        return Ok(resultado);
    }

    /// <summary>Obtém detalhe de um admin pelo ID.</summary>
    /// <response code="200">Detalhes do admin.</response>
    /// <response code="404">Não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminDetalheDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var dto = await _obter.Handle(new ObterAdminQuery(id));
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Cria novo admin com senha temporária e force_password_reset = true.</summary>
    /// <response code="201">Admin criado. Retorna ID, e-mail, nome e senha temporária (única vez).</response>
    /// <response code="422">E-mail duplicado ou validação falhou.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AdminCriadoResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarAdminRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await _criar.Handle(
            new CriarAdminCommand(adminId, request.Nome, request.Email, request.Motivo), ct);

        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }

    /// <summary>Desativa admin. 422 se for o último ativo.</summary>
    /// <response code="204">Desativado com sucesso.</response>
    /// <response code="422">Último admin ativo ou validação falhou.</response>
    [HttpPost("{id:guid}/desativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desativar(Guid id, [FromBody] MotivoRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")!.Value);
        await _desativar.Handle(new DesativarAdminCommand(adminId, id, request.Motivo), ct);
        return NoContent();
    }

    /// <summary>Reativa admin previamente desativado.</summary>
    /// <response code="204">Reativado com sucesso.</response>
    /// <response code="422">Admin já ativo ou não encontrado.</response>
    [HttpPost("{id:guid}/reativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reativar(Guid id, [FromBody] MotivoRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")!.Value);
        await _reativar.Handle(new ReativarAdminCommand(adminId, id, request.Motivo), ct);
        return NoContent();
    }

    /// <summary>Gera nova senha temporária e revoga sessões. 422 para auto-reset.</summary>
    /// <response code="200">Senha temporária gerada (única vez).</response>
    /// <response code="422">Auto-reset não permitido ou validação falhou.</response>
    [HttpPost("{id:guid}/reset-senha")]
    [ProducesResponseType(typeof(SenhaResetadaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetSenha(Guid id, [FromBody] MotivoRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await _resetSenha.Handle(new ResetSenhaAdminCommand(adminId, id, request.Motivo), ct);
        return Ok(result);
    }
}

// Request bodies inline — simples o suficiente para não precisar de namespace próprio.
public record CriarAdminRequest(string Nome, string Email, string Motivo);
public record MotivoRequest(string Motivo);
