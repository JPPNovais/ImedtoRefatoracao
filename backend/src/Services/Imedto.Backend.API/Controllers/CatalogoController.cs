using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Filters;

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
    /// Liberado antes do onboarding — o próprio fluxo de onboarding usa esta lista.
    /// </summary>
    [HttpGet("profissoes")]
    [AllowBeforeOnboarding]
    public async Task<ActionResult<IEnumerable<ProfissaoListadaDto>>> ListarProfissoes(
        [FromQuery] bool ativas = true)
    {
        var result = await _query.Query<ListarProfissoesQuery, IEnumerable<ProfissaoListadaDto>>(
            new ListarProfissoesQuery { ApenasAtivas = ativas });
        return Ok(result);
    }

    /// <summary>
    /// Lista especialidades filtradas por profissão (obrigatório).
    /// Liberado antes do onboarding — o próprio fluxo de onboarding usa esta lista.
    /// </summary>
    [HttpGet("especialidades")]
    [AllowBeforeOnboarding]
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

    /// <summary>
    /// Busca procedimentos do catálogo TUSS/CBHPM por código ou nome (autocomplete).
    /// Sem RequiresEstabelecimento — ref data global.
    /// </summary>
    [HttpGet("procedimentos")]
    public async Task<ActionResult<IEnumerable<ProcedimentoCatalogoDto>>> BuscarProcedimentos(
        [FromQuery] string? termo,
        [FromQuery] string? origem,
        [FromQuery] int limit = 20)
    {
        var result = await _query.Query<BuscarProcedimentoCatalogoQuery, IEnumerable<ProcedimentoCatalogoDto>>(
            new BuscarProcedimentoCatalogoQuery { Termo = termo, Origem = origem, Limit = limit });
        return Ok(result);
    }

    /// <summary>
    /// Obtém um procedimento do catálogo pelo código TUSS/CBHPM.
    /// </summary>
    [HttpGet("procedimentos/{codigo}")]
    public async Task<ActionResult<ProcedimentoCatalogoDto>> ObterProcedimento([FromRoute] string codigo)
    {
        var result = await _query.Query<ObterProcedimentoPorCodigoQuery, ProcedimentoCatalogoDto?>(
            new ObterProcedimentoPorCodigoQuery { Codigo = codigo });
        if (result is null)
            throw new BusinessException("Procedimento não encontrado.");
        return Ok(result);
    }
}
