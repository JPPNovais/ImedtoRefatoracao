using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Item 4.2 — Solicitação inversa de vínculo (profissional → estabelecimento).
/// Complementa o fluxo "clínica convida profissional" do <see cref="VinculoController"/>.
///
/// Endpoints divididos em dois grupos:
/// - "Minhas solicitações": ações do profissional (criar, listar, cancelar). Não exigem
///   <c>X-Estabelecimento-Id</c> — o profissional ainda não é tenant deste estabelecimento.
/// - "Recebidas": ações do dono (listar, aprovar, recusar). Exigem <c>RequiresEstabelecimento</c>
///   e o handler valida que o solicitante é o dono.
/// </summary>
[Authorize]
[ApiController]
[Route("api/solicitacoes-vinculo")]
[Produces("application/json")]
public class SolicitacaoVinculoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public SolicitacaoVinculoController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Profissional pede acesso a um estabelecimento.</summary>
    /// <response code="201">Solicitação criada.</response>
    /// <response code="422">Já existe vínculo/convite ou solicitação pendente.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Solicitar([FromBody] SolicitarVinculoRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new SolicitarVinculoCommand
        {
            ProfissionalUsuarioId = userId,
            EstabelecimentoId = request.EstabelecimentoId,
            Mensagem = request.Mensagem
        });

        return Created(string.Empty, null);
    }

    /// <summary>Lista as solicitações enviadas pelo profissional logado.</summary>
    [HttpGet("minhas")]
    [ProducesResponseType(typeof(IEnumerable<SolicitacaoVinculoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMinhas()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var result = await _requestBus.Query<ListarMinhasSolicitacoesVinculoQuery, IEnumerable<SolicitacaoVinculoDto>>(
            new ListarMinhasSolicitacoesVinculoQuery { ProfissionalUsuarioId = userId });

        return Ok(result);
    }

    /// <summary>Lista as solicitações recebidas pelo estabelecimento ativo (apenas dono).</summary>
    [HttpGet("recebidas")]
    [RequiresEstabelecimento]
    [ProducesResponseType(typeof(IEnumerable<SolicitacaoVinculoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ListarRecebidas([FromQuery] string status = null)
    {
        var result = await _requestBus.Query<ListarSolicitacoesVinculoRecebidasQuery, IEnumerable<SolicitacaoVinculoDto>>(
            new ListarSolicitacoesVinculoRecebidasQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                UsuarioSolicitanteId = _tenant.UsuarioId,
                Status = status
            });

        return Ok(result);
    }

    /// <summary>Aprovar solicitação (cria o vínculo automaticamente). Apenas dono.</summary>
    [HttpPost("{id:long}/aprovar")]
    [RequiresEstabelecimento]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Aprovar(long id)
    {
        await _commandBus.Send(new AprovarSolicitacaoVinculoCommand
        {
            SolicitacaoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            AprovadoPorUsuarioId = _tenant.UsuarioId
        });

        return NoContent();
    }

    /// <summary>Recusar solicitação. Apenas dono. Motivo é opcional mas recomendado.</summary>
    [HttpPost("{id:long}/recusar")]
    [RequiresEstabelecimento]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Recusar(long id, [FromBody] RecusarSolicitacaoRequest request)
    {
        await _commandBus.Send(new RecusarSolicitacaoVinculoCommand
        {
            SolicitacaoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            RecusadoPorUsuarioId = _tenant.UsuarioId,
            Motivo = request?.Motivo
        });

        return NoContent();
    }

    /// <summary>Cancelar solicitação pendente — apenas o próprio profissional.</summary>
    [HttpPost("{id:long}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancelar(long id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new CancelarSolicitacaoVinculoCommand
        {
            SolicitacaoId = id,
            SolicitanteUsuarioId = userId
        });

        return NoContent();
    }
}

public record SolicitarVinculoRequest(long EstabelecimentoId, string Mensagem);
public record RecusarSolicitacaoRequest(string Motivo);
