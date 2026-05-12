using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Queries;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/automacoes")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
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

    // ---- Item 2.2: Engine de automações (regras + fila de eventos) ----
    // Apenas o dono pode criar/editar regras (validação espelhada nos handlers).
    // Listagens são abertas para qualquer membro do tenant — útil para o profissional
    // ver o que foi configurado, mas eles não conseguem editar.

    /// <summary>Lista as regras de automação configuradas no estabelecimento atual.</summary>
    [HttpGet("regras")]
    [ProducesResponseType(typeof(IEnumerable<RegraAutomacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarRegras()
    {
        var resultado = await _queries.Query<ListarRegrasAutomacaoQuery, IEnumerable<RegraAutomacaoDto>>(
            new ListarRegrasAutomacaoQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(resultado);
    }

    /// <summary>Cria uma nova regra de automação. Dono ou usuário com permissão de automação.</summary>
    [HttpPost("regras")]
    [RequiresPermissaoExtra(PermissoesExtras.AutomacaoConfig)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CriarRegra([FromBody] CriarRegraAutomacaoCommand command)
    {
        command.EstabelecimentoId = _tenant.EstabelecimentoId;
        command.SolicitanteUsuarioId = _tenant.UsuarioId;
        await _commands.Send(command);
        return Created(string.Empty, null);
    }

    /// <summary>Atualiza uma regra existente. Dono ou usuário com permissão de automação.</summary>
    [HttpPut("regras/{id:long}")]
    [RequiresPermissaoExtra(PermissoesExtras.AutomacaoConfig)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarRegra(long id, [FromBody] AtualizarRegraAutomacaoCommand command)
    {
        command.RegraId = id;
        command.EstabelecimentoId = _tenant.EstabelecimentoId;
        command.SolicitanteUsuarioId = _tenant.UsuarioId;
        await _commands.Send(command);
        return NoContent();
    }

    /// <summary>Ativa uma regra desativada. Dono ou usuário com permissão de automação.</summary>
    [HttpPost("regras/{id:long}/ativar")]
    [RequiresPermissaoExtra(PermissoesExtras.AutomacaoConfig)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtivarRegra(long id)
    {
        await _commands.Send(new AtivarRegraAutomacaoCommand
        {
            RegraId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    /// <summary>Desativa uma regra ativa (mantém histórico). Dono ou usuário com permissão de automação.</summary>
    [HttpPost("regras/{id:long}/desativar")]
    [RequiresPermissaoExtra(PermissoesExtras.AutomacaoConfig)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DesativarRegra(long id)
    {
        await _commands.Send(new DesativarRegraAutomacaoCommand
        {
            RegraId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    /// <summary>Lista a fila de eventos de automação — debugging/observabilidade.</summary>
    [HttpGet("eventos")]
    [ProducesResponseType(typeof(IEnumerable<EventoAutomacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarEventos(
        [FromQuery] string? status = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 50)
    {
        var resultado = await _queries.Query<ListarEventosAutomacaoQuery, IEnumerable<EventoAutomacaoDto>>(
            new ListarEventosAutomacaoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Status = status,
                Pagina = pagina,
                Tamanho = tamanho
            });
        return Ok(resultado);
    }
}
