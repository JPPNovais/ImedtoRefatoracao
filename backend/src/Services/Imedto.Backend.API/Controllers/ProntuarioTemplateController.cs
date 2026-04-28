using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Templates de prontuário (modelos) + pool de variáveis (alergias, medicamentos, etc.).
/// Todos os endpoints são escopados pelo tenant via <see cref="RequiresEstabelecimentoAttribute"/>.
/// A ferramenta admin gerencia padrão-sistema por canal separado.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Route("api/prontuario")]
[Produces("application/json")]
public class ProntuarioTemplateController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public ProntuarioTemplateController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Lista modelos disponíveis (padrão-sistema + do estabelecimento).</summary>
    [HttpGet("modelos")]
    [ProducesResponseType(typeof(IEnumerable<ModeloProntuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarModelos([FromQuery] bool apenasAtivos = true)
    {
        var lista = await _requestBus.Query<ListarModelosDisponiveisQuery, IEnumerable<ModeloProntuarioDto>>(
            new ListarModelosDisponiveisQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ApenasAtivos = apenasAtivos
            });
        return Ok(lista);
    }

    /// <summary>Retorna um modelo visível para o estabelecimento (padrão-sistema ou próprio).</summary>
    [HttpGet("modelos/{id:long}")]
    [ProducesResponseType(typeof(ModeloProntuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterModelo(long id)
    {
        var dto = await _requestBus.Query<ObterModeloDeProntuarioQuery, ModeloProntuarioDto>(
            new ObterModeloDeProntuarioQuery
            {
                ModeloId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Cria um novo modelo próprio do estabelecimento.</summary>
    [HttpPost("modelos")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CriarModelo([FromBody] ModeloRequest request)
    {
        await _commandBus.Send(new CriarModeloDeProntuarioCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = request.Nome,
            Descricao = request.Descricao,
            EstruturaJson = request.EstruturaJson
        });
        return Created(string.Empty, null);
    }

    /// <summary>Atualiza modelo próprio (não pode editar padrão-sistema).</summary>
    [HttpPut("modelos/{id:long}")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarModelo(long id, [FromBody] ModeloRequest request)
    {
        await _commandBus.Send(new AtualizarModeloDeProntuarioCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = request.Nome,
            Descricao = request.Descricao,
            EstruturaJson = request.EstruturaJson
        });
        return NoContent();
    }

    /// <summary>Exclui modelo próprio do estabelecimento (não pode excluir padrão-sistema). Apenas Dono.</summary>
    [HttpDelete("modelos/{id:long}")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExcluirModelo(long id)
    {
        await _commandBus.Send(new ExcluirModeloDeProntuarioCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    /// <summary>Lista itens do pool de variáveis (opcionalmente filtrado por tipo).</summary>
    [HttpGet("pool")]
    [ProducesResponseType(typeof(IEnumerable<VariavelPoolDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPool(
        [FromQuery] string tipo = null,
        [FromQuery] bool apenasAtivos = true)
    {
        var lista = await _requestBus.Query<ListarVariaveisPoolQuery, IEnumerable<VariavelPoolDto>>(
            new ListarVariaveisPoolQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Tipo = tipo,
                ApenasAtivos = apenasAtivos
            });
        return Ok(lista);
    }

    /// <summary>Adiciona um item ao pool (escopo do estabelecimento). Apenas Dono.</summary>
    [HttpPost("pool")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AdicionarPool([FromBody] AdicionarPoolRequest request)
    {
        await _commandBus.Send(new AdicionarVariavelPoolCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Tipo = request.Tipo,
            Nome = request.Nome
        });
        return Created(string.Empty, null);
    }

    /// <summary>Renomeia um item customizado do pool (não permite editar padrão-sistema). Apenas Dono.</summary>
    [HttpPut("pool/{id:long}")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarPool(long id, [FromBody] AtualizarPoolRequest request)
    {
        await _commandBus.Send(new AtualizarVariavelPoolCommand
        {
            ItemId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = request.Nome
        });
        return NoContent();
    }

    /// <summary>Exclui um item customizado do pool (não permite excluir padrão-sistema). Apenas Dono.</summary>
    [HttpDelete("pool/{id:long}")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExcluirPool(long id)
    {
        await _commandBus.Send(new ExcluirVariavelPoolCommand
        {
            ItemId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }
}

public record ModeloRequest(string Nome, string Descricao, string EstruturaJson);
public record AdicionarPoolRequest(string Tipo, string Nome);
public record AtualizarPoolRequest(string Nome);
