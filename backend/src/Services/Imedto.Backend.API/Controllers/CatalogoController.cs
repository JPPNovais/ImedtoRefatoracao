using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/catalogo")]
[Authorize]
public class CatalogoController : ControllerBase
{
    private readonly IRequestBus _query;

    public CatalogoController(IRequestBus query) => _query = query;

    /// <summary>
    /// Lista profissões do catálogo global.
    /// </summary>
    [HttpGet("profissoes")]
    public async Task<ActionResult<IEnumerable<ProfissaoListadaDto>>> ListarProfissoes(
        [FromQuery] bool ativas = true)
    {
        var result = await _query.Query<ListarProfissoesQuery, IEnumerable<ProfissaoListadaDto>>(
            new ListarProfissoesQuery { ApenasAtivas = ativas });
        return Ok(result);
    }

    /// <summary>
    /// Lista especialidades filtradas por profissão (obrigatório).
    /// </summary>
    [HttpGet("especialidades")]
    public async Task<ActionResult<IEnumerable<EspecialidadeListadaDto>>> ListarEspecialidades(
        [FromQuery] long profissaoId,
        [FromQuery] bool ativas = true)
    {
        if (profissaoId <= 0)
            return BadRequest("profissaoId é obrigatório.");

        var result = await _query.Query<ListarEspecialidadesQuery, IEnumerable<EspecialidadeListadaDto>>(
            new ListarEspecialidadesQuery { ProfissaoId = profissaoId, ApenasAtivas = ativas });
        return Ok(result);
    }

    /// <summary>
    /// Lista regiões anatômicas do catálogo global (ref data para o BodyMapSvg).
    /// Sem RequiresEstabelecimento — é catálogo global.
    /// </summary>
    [HttpGet("regioes-anatomicas")]
    public async Task<ActionResult<IEnumerable<RegiaoCatalogoDto>>> ListarRegioesAnatomicas(
        [FromQuery] string? vista,
        [FromQuery] bool ativas = true)
    {
        var result = await _query.Query<ListarRegioesCatalogoQuery, IEnumerable<RegiaoCatalogoDto>>(
            new ListarRegioesCatalogoQuery { Vista = vista, ApenasAtivas = ativas });
        return Ok(result);
    }
}
