using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Prontuário de um paciente específico. Exige papel Profissional ou Dono
/// (dados clínicos sensíveis — Recepcionista não tem acesso).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[ApiController]
[Route("api/paciente/{pacienteId:long}/prontuario")]
[Produces("application/json")]
public class ProntuarioController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public ProntuarioController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Retorna o prontuário do paciente + timeline de evoluções (404 se nunca iniciado).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProntuarioCompletoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long pacienteId, [FromQuery] int timeline = 50)
    {
        var dto = await _requestBus.Query<ObterProntuarioDoPacienteQuery, ProntuarioCompletoDto>(
            new ObterProntuarioDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                TamanhoTimeline = timeline
            });

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Inicia o prontuário do paciente com um modelo (padrão-sistema ou próprio).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Iniciar(long pacienteId, [FromBody] IniciarProntuarioRequest request)
    {
        await _commandBus.Send(new IniciarProntuarioCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ModeloDeProntuarioId = request.ModeloDeProntuarioId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });
        return Created(string.Empty, null);
    }

    /// <summary>Registra uma nova evolução (append-only).</summary>
    [HttpPost("evolucoes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarEvolucao(long pacienteId, [FromBody] RegistrarEvolucaoRequest request)
    {
        await _commandBus.Send(new RegistrarEvolucaoCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            AutorUsuarioId = _tenant.UsuarioId,
            ConteudoJson = request.ConteudoJson,
            ModeloDeProntuarioId = request.ModeloDeProntuarioId
        });
        return Created(string.Empty, null);
    }
}

public record IniciarProntuarioRequest(long ModeloDeProntuarioId);
public record RegistrarEvolucaoRequest(string ConteudoJson, long? ModeloDeProntuarioId = null);
