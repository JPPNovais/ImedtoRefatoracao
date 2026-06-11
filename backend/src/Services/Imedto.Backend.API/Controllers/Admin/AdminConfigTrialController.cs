using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.ConfigTrial;
using Imedto.Backend.Contracts.Admin.ConfigTrial.Commands;
using Imedto.Backend.Contracts.Admin.ConfigTrial.Queries;
using Imedto.Backend.Contracts.Admin.ConfigTrial.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Configuração global de trial automático para novos estabelecimentos (F4/CA30).
/// Singleton: uma única configuração para toda a instância do produto.
/// </summary>
[ApiController]
[Route("api/admin/config-trial")]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminConfigTrialController : ControllerBase
{
    private readonly ObterConfigTrialAdminQueryHandler _obter;
    private readonly AtualizarConfigTrialAdminCommandHandler _atualizar;

    public AdminConfigTrialController(
        ObterConfigTrialAdminQueryHandler obter,
        AtualizarConfigTrialAdminCommandHandler atualizar)
    {
        _obter = obter;
        _atualizar = atualizar;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ConfigTrialAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter(CancellationToken ct = default)
    {
        var result = await _obter.Handle(new ObterConfigTrialAdminQuery(), ct);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
        [FromBody] AtualizarConfigTrialRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _atualizar.Handle(new AtualizarConfigTrialAdminCommand(
            request.PlanoTrialId,
            request.DuracaoTrialDias,
            request.TrialHabilitado,
            request.Motivo,
            adminId), ct);
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

public record AtualizarConfigTrialRequest(
    Guid PlanoTrialId,
    int DuracaoTrialDias,
    bool TrialHabilitado,
    string Motivo);
