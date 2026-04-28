using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Contracts.Unidades.Queries;
using Imedto.Backend.Contracts.Unidades.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>Unidades físicas (matriz/filial) do estabelecimento.</summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Route("api/estabelecimento/{estabelecimentoId:long}/unidades")]
[Produces("application/json")]
public class UnidadeController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public UnidadeController(ICommandBus commandBus, IRequestBus requestBus, ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Lista as unidades do estabelecimento.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UnidadeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(long estabelecimentoId)
    {
        var resultado = await _requestBus.Query<ListarUnidadesQuery, IEnumerable<UnidadeDto>>(
            new ListarUnidadesQuery
            {
                EstabelecimentoId     = _tenant.EstabelecimentoId,
                UsuarioSolicitanteId  = _tenant.UsuarioId,
            });

        return Ok(resultado);
    }

    /// <summary>Cria uma unidade. Apenas o dono pode.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(long estabelecimentoId, [FromBody] CriarUnidadeRequest request)
    {
        await _commandBus.Send(new CriarUnidadeCommand
        {
            EstabelecimentoId    = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            Nome                 = request.Nome,
            IsPrincipal          = request.IsPrincipal,
            Cep                  = request.Cep,
            Logradouro           = request.Logradouro,
            Numero               = request.Numero,
            Complemento          = request.Complemento,
            Bairro               = request.Bairro,
            Cidade               = request.Cidade,
            Estado               = request.Estado,
            Telefone             = request.Telefone,
        });

        return Created(string.Empty, null);
    }

    /// <summary>Atualiza uma unidade. Apenas o dono pode.</summary>
    [HttpPut("{unidadeId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long estabelecimentoId, long unidadeId, [FromBody] AtualizarUnidadeRequest request)
    {
        await _commandBus.Send(new AtualizarUnidadeCommand
        {
            UnidadeId            = unidadeId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            Nome                 = request.Nome,
            IsPrincipal          = request.IsPrincipal,
            Cep                  = request.Cep,
            Logradouro           = request.Logradouro,
            Numero               = request.Numero,
            Complemento          = request.Complemento,
            Bairro               = request.Bairro,
            Cidade               = request.Cidade,
            Estado               = request.Estado,
            Telefone             = request.Telefone,
        });

        return NoContent();
    }

    /// <summary>Exclui uma unidade. Apenas o dono pode.</summary>
    [HttpDelete("{unidadeId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Excluir(long estabelecimentoId, long unidadeId)
    {
        await _commandBus.Send(new DeletarUnidadeCommand
        {
            UnidadeId            = unidadeId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });

        return NoContent();
    }
}

public record CriarUnidadeRequest(
    string Nome,
    bool IsPrincipal,
    string Cep,
    string Logradouro,
    string Numero,
    string Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Telefone);

public record AtualizarUnidadeRequest(
    string Nome,
    bool IsPrincipal,
    string Cep,
    string Logradouro,
    string Numero,
    string Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Telefone);
