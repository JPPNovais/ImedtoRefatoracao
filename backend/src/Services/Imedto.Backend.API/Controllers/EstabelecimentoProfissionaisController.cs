using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;

namespace Imedto.Backend.API.Controllers;

/// <summary>Listagens relacionadas a vínculos: profissionais de um estabelecimento e convites do usuário.</summary>
[Authorize]
[ApiController]
[Produces("application/json")]
public class EstabelecimentoProfissionaisController : ControllerBase
{
    private readonly IRequestBus _requestBus;

    public EstabelecimentoProfissionaisController(IRequestBus requestBus)
    {
        _requestBus = requestBus;
    }

    /// <summary>Dono lista os profissionais (convidados + ativos) do seu estabelecimento.</summary>
    [HttpGet("/api/estabelecimento/{estabelecimentoId:long}/profissionais")]
    [ProducesResponseType(typeof(IEnumerable<ProfissionalVinculadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ListarProfissionais(long estabelecimentoId)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var resultado = await _requestBus.Query<ListarProfissionaisEstabelecimentoQuery, IEnumerable<ProfissionalVinculadoDto>>(
            new ListarProfissionaisEstabelecimentoQuery
            {
                EstabelecimentoId = estabelecimentoId,
                UsuarioSolicitanteId = userId
            });

        return Ok(resultado);
    }

    /// <summary>Usuário lista seus convites pendentes.</summary>
    [AllowBeforeOnboarding]
    [HttpGet("/api/vinculo/convites/me")]
    [ProducesResponseType(typeof(IEnumerable<ConviteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMeusConvites()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var resultado = await _requestBus.Query<ListarMeusConvitesQuery, IEnumerable<ConviteDto>>(
            new ListarMeusConvitesQuery { UsuarioId = userId });

        return Ok(resultado);
    }
}
