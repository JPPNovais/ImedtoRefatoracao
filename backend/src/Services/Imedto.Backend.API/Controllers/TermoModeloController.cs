using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Modelos (templates) de termo de consentimento. CRUD restrito a quem tem
/// <c>termos.gerenciar_modelos</c> — listagem aceita também
/// <c>termos.emitir</c> (precisa ver os modelos pra emitir).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Produces("application/json")]
public class TermoModeloController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public TermoModeloController(ICommandBus commandBus, IRequestBus requestBus, ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>
    /// Lista paginada de modelos do estabelecimento + (opcional) padrões do sistema.
    /// </summary>
    [HttpGet("api/termos/modelos")]
    [RequiresAcao("termos")] // qualquer ação da área concede leitura — handler valida granularidade
    [ProducesResponseType(typeof(PaginaModelosTermoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string busca = null,
        [FromQuery] string categoria = null,
        [FromQuery] bool somenteAtivos = false,
        [FromQuery] bool incluirPadroes = true,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var dto = await _requestBus.Query<ListarModelosTermoQuery, PaginaModelosTermoDto>(new ListarModelosTermoQuery
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Busca = busca,
            Categoria = categoria,
            SomenteAtivos = somenteAtivos,
            IncluirPadroes = incluirPadroes,
            Pagina = pagina,
            Tamanho = tamanho,
        });
        return Ok(dto);
    }

    /// <summary>Lista somente modelos padrão do sistema (ativos).</summary>
    [HttpGet("api/termos/modelos/padroes")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(typeof(IReadOnlyList<TermoModeloDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPadroes()
    {
        var dto = await _requestBus.Query<ListarModelosPadraoTermoQuery, IReadOnlyList<TermoModeloDto>>(
            new ListarModelosPadraoTermoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
            });
        return Ok(dto);
    }

    [HttpGet("api/termos/modelos/{id:long}")]
    [RequiresAcao("termos")]
    [ProducesResponseType(typeof(TermoModeloDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Obter(long id)
    {
        var dto = await _requestBus.Query<ObterModeloTermoQuery, TermoModeloDto>(new ObterModeloTermoQuery
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        });
        return Ok(dto);
    }

    [HttpGet("api/termos/variaveis")]
    [RequiresAcao("termos")]
    [ProducesResponseType(typeof(IReadOnlyList<VariavelDisponivelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarVariaveis()
    {
        var dto = await _requestBus.Query<ListarVariaveisDisponiveisQuery, IReadOnlyList<VariavelDisponivelDto>>(
            new ListarVariaveisDisponiveisQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(dto);
    }

    [HttpPost("api/termos/modelos")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarModeloRequest request)
    {
        var cmd = new CriarModeloTermoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Categoria = request.Categoria,
            Titulo = request.Titulo,
            ConteudoHtml = request.ConteudoHtml,
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.ModeloIdCriado }, new { modeloId = cmd.ModeloIdCriado });
    }

    [HttpPut("api/termos/modelos/{id:long}")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarModeloRequest request)
    {
        await _commandBus.Send(new AtualizarModeloTermoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Categoria = request.Categoria,
            Titulo = request.Titulo,
            ConteudoHtml = request.ConteudoHtml,
        });
        return NoContent();
    }

    [HttpPatch("api/termos/modelos/{id:long}/ativo")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AlterarAtivo(long id, [FromBody] AlterarAtivoRequest request)
    {
        await _commandBus.Send(new AlterarAtivoModeloTermoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Ativo = request.Ativo,
        });
        return NoContent();
    }

    [HttpDelete("api/termos/modelos/{id:long}")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(long id)
    {
        await _commandBus.Send(new ExcluirModeloTermoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        });
        return NoContent();
    }

    [HttpPost("api/termos/modelos/{id:long}/clonar")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Clonar(long id)
    {
        var cmd = new ClonarModeloTermoCommand
        {
            ModeloPadraoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.ModeloIdClonado }, new { modeloId = cmd.ModeloIdClonado });
    }
}

public record CriarModeloRequest(string Categoria, string Titulo, string ConteudoHtml);
public record AtualizarModeloRequest(string Categoria, string Titulo, string ConteudoHtml);
public record AlterarAtivoRequest(bool Ativo);
