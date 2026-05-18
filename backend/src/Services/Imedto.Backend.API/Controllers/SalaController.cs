using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Contracts.Salas.Queries;
using Imedto.Backend.Contracts.Salas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>Repartições (salas de atendimento) do estabelecimento.</summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Route("api/estabelecimento/{estabelecimentoId:long}/salas")]
[Produces("application/json")]
public class SalaController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public SalaController(ICommandBus commandBus, IRequestBus requestBus, ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Lista as repartições do estabelecimento (com nome de unidade e tipo).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SalaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(long estabelecimentoId, [FromQuery] bool apenasAtivas = false)
    {
        var resultado = await _requestBus.Query<ListarSalasQuery, IEnumerable<SalaDto>>(
            new ListarSalasQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                UsuarioSolicitanteId = _tenant.UsuarioId,
                ApenasAtivas = apenasAtivas,
            });

        return Ok(resultado);
    }

    /// <summary>Cria uma repartição. Apenas o dono pode.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(long estabelecimentoId, [FromBody] CriarSalaRequest request)
    {
        await _commandBus.Send(new CriarSalaCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            UnidadeId = request.UnidadeId,
            TipoSalaId = request.TipoSalaId,
            Nome = request.Nome,
            Descricao = request.Descricao,
        });

        return Created(string.Empty, null);
    }

    /// <summary>Atualiza uma repartição. Apenas o dono pode.</summary>
    [HttpPut("{salaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long estabelecimentoId, long salaId, [FromBody] AtualizarSalaRequest request)
    {
        await _commandBus.Send(new AtualizarSalaCommand
        {
            SalaId = salaId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            UnidadeId = request.UnidadeId,
            TipoSalaId = request.TipoSalaId,
            Nome = request.Nome,
            Descricao = request.Descricao,
        });

        return NoContent();
    }

    /// <summary>Exclui uma repartição. Apenas o dono pode.</summary>
    [HttpDelete("{salaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Excluir(long estabelecimentoId, long salaId)
    {
        await _commandBus.Send(new DeletarSalaCommand
        {
            SalaId = salaId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });

        return NoContent();
    }

    /// <summary>Desativa uma repartição. Apenas o dono pode.</summary>
    [HttpPut("{salaId:long}/desativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Desativar(long estabelecimentoId, long salaId)
    {
        await _commandBus.Send(new DesativarSalaCommand
        {
            SalaId = salaId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });
        return NoContent();
    }

    /// <summary>Reativa uma repartição. Apenas o dono pode.</summary>
    [HttpPut("{salaId:long}/reativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reativar(long estabelecimentoId, long salaId)
    {
        await _commandBus.Send(new ReativarSalaCommand
        {
            SalaId = salaId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });
        return NoContent();
    }
}

/// <summary>Catálogo de tipos de sala (system-wide).</summary>
[Authorize]
[ApiController]
[Route("api/tipos-sala")]
[Produces("application/json")]
public class TiposSalaController : ControllerBase
{
    private readonly IRequestBus _requestBus;

    public TiposSalaController(IRequestBus requestBus)
    {
        _requestBus = requestBus;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TipoSalaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _requestBus.Query<ListarTiposSalaQuery, IEnumerable<TipoSalaDto>>(
            new ListarTiposSalaQuery());
        return Ok(resultado);
    }
}

public record CriarSalaRequest(long UnidadeId, long? TipoSalaId, string Nome, string Descricao);

public record AtualizarSalaRequest(long UnidadeId, long? TipoSalaId, string Nome, string Descricao);
