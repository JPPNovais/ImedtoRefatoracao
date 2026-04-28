using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/automacoes")]
[Authorize]
[RequiresEstabelecimento]
public class AutomacaoController : ControllerBase
{
    private readonly ICommandBus _commands;
    private readonly IRequestBus _queries;
    private readonly ICurrentTenantAccessor _tenant;

    public AutomacaoController(ICommandBus commands, IRequestBus queries, ICurrentTenantAccessor tenant)
    {
        _commands = commands;
        _queries = queries;
        _tenant = tenant;
    }

    [HttpGet("configuracao")]
    public async Task<ActionResult<ConfiguracaoAutomacaoDto>> ObterConfiguracao()
    {
        var result = await _queries.Query<ObterConfiguracaoAutomacaoQuery, ConfiguracaoAutomacaoDto>(
            new ObterConfiguracaoAutomacaoQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(result);
    }

    [HttpPut("configuracao")]
    public async Task<IActionResult> SalvarConfiguracao([FromBody] SalvarConfiguracaoAutomacaoCommand command)
    {
        command.EstabelecimentoId = _tenant.EstabelecimentoId;
        await _commands.Send(command);
        return NoContent();
    }

    [HttpPost("expirar-orcamentos")]
    public async Task<IActionResult> ExpirarOrcamentos()
    {
        await _commands.Send(new ExpirarOrcamentosVencidosCommand());
        return NoContent();
    }

    [HttpPost("enviar-lembretes")]
    public async Task<IActionResult> EnviarLembretes()
    {
        await _commands.Send(new EnviarLembretesAgendamentosCommand());
        return NoContent();
    }
}
