using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Configs;
using Imedto.Backend.Contracts.Admin.Configs.Commands;
using Imedto.Backend.Contracts.Admin.Configs.Queries;
using Imedto.Backend.Contracts.Admin.Configs.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Gerenciamento de configurações globais do sistema (admin global).
/// W2-CA6 a W2-CA13.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminConfigsController : ControllerBase
{
    private readonly ListarConfigsAdminQueryHandler _listar;
    private readonly AtualizarConfigAdminCommandHandler _atualizar;

    public AdminConfigsController(
        ListarConfigsAdminQueryHandler listar,
        AtualizarConfigAdminCommandHandler atualizar)
    {
        _listar = listar;
        _atualizar = atualizar;
    }

    [HttpGet("api/admin/configs")]
    [ProducesResponseType(typeof(IReadOnlyList<SecaoConfigsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct = default)
    {
        var resultado = await _listar.Handle(new ListarConfigsAdminQuery(), ct);
        return Ok(resultado);
    }

    [HttpPut("api/admin/configs/{chave}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
        string chave,
        [FromBody] AtualizarConfigRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarConfigAdminCommand(chave, request.Valor, request.Motivo, adminId), ct);
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

public record AtualizarConfigRequest(string Valor, string Motivo);
