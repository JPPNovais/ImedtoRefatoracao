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
/// Pendências de atendimento de um paciente (F3B — briefing 2026-06-10_012).
/// GET: prontuario.ver. POST concluir: prontuario.editar (CA70).
/// Multi-tenant: todos os endpoints filtram pelo estabelecimento do tenant ativo (R5/CA69).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
[ApiController]
[Route("api/paciente/{pacienteId:long}/pendencias")]
[Produces("application/json")]
public class PendenciaAtendimentoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public PendenciaAtendimentoController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>
    /// Lista pendências abertas (status=Pendente) do paciente no tenant.
    /// Alimenta o painel persistente em PacienteDetalheView (CA68/CA74).
    /// Requer prontuario.ver.
    /// </summary>
    [HttpGet]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono, TenantPapel.Recepcionista)]
    [RequiresAcao("prontuario.ver")]
    [ProducesResponseType(typeof(IReadOnlyList<PendenciaAbertaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(long pacienteId)
    {
        var resultado = await _requestBus.Query<ListarPendenciasAbertasQuery, IReadOnlyList<PendenciaAbertaDto>>(
            new ListarPendenciasAbertasQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
            });
        return Ok(resultado);
    }

    /// <summary>
    /// Conclui manualmente uma pendência pelo painel (R14/CA67).
    /// Requer prontuario.editar (CA70).
    /// </summary>
    [HttpPost("{pendenciaId:long}/concluir")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono, TenantPapel.Recepcionista)]
    [RequiresAcao("prontuario.editar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConcluirManual(long pacienteId, long pendenciaId)
    {
        await _commandBus.Send(new ConcluirPendenciaManualCommand
        {
            PendenciaId = pendenciaId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
        });
        return NoContent();
    }
}
