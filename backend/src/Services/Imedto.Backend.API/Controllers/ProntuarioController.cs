using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
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
[RequiresAssinaturaAtiva]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[RequiresAcao("prontuario")]
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

    /// <summary>
    /// Retorna o prontuário do paciente + timeline de evoluções.
    /// Quando o paciente ainda não tem prontuário iniciado, retorna 200 com body
    /// <c>null</c> — antes era 404, mas isso polui o console do browser com
    /// "Failed to load resource" e dificulta separar erros reais de "ainda não
    /// iniciado". O front trata null como "exibir CTA de iniciar".
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProntuarioCompletoDto), StatusCodes.Status200OK)]
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

        return Ok(dto);
    }

    /// <summary>
    /// Retorna apenas o total de evoluções (badge/contador). Resposta enxuta para UIs
    /// que só precisam do número, sem trafegar a timeline inteira.
    /// </summary>
    [HttpGet("contagem-evolucoes")]
    [ProducesResponseType(typeof(ContagemEvolucoesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ContarEvolucoes(long pacienteId)
    {
        var dto = await _requestBus.Query<ContarEvolucoesProntuarioPacienteQuery, ContagemEvolucoesDto>(
            new ContarEvolucoesProntuarioPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });
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
