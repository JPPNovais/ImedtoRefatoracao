using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints de leitura de assinatura/plano.
///
/// <para><c>GET /api/minha-assinatura</c>: detalhe da assinatura do estabelecimento ativo.
/// Apenas o dono pode acessar (assinatura é dado financeiro/contratual — não cabe ao
/// profissional vinculado). LGPD: a query devolve apenas plano + status + datas; sem dados
/// pessoais do dono.</para>
///
/// <para><c>GET /api/planos</c>: catálogo de planos ativos. Qualquer usuário autenticado pode
/// listar (precisa para escolher plano). Não exige tenant.</para>
/// </summary>
[Authorize]
[ApiController]
[Produces("application/json")]
public class AssinaturaController : ControllerBase
{
    private readonly IRequestBus _requestBus;

    public AssinaturaController(IRequestBus requestBus)
    {
        _requestBus = requestBus;
    }

    /// <summary>Retorna a assinatura do estabelecimento ativo (header X-Estabelecimento-Id). 404 se não houver.</summary>
    /// <response code="200">Assinatura vigente do estabelecimento.</response>
    /// <response code="403">Apenas o dono pode visualizar a assinatura.</response>
    /// <response code="404">Estabelecimento sem assinatura registrada.</response>
    [HttpGet("/api/minha-assinatura")]
    [RequiresEstabelecimento]
    [ProducesResponseType(typeof(AssinaturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterMinhaAssinatura([FromServices] ICurrentTenantAccessor tenant)
    {
        // Apenas o dono pode ver/editar dados de assinatura — profissionais vinculados
        // não veem informações financeiras do estabelecimento (LGPD/contrato).
        if (!tenant.EhDono)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                tipo = "SemAcesso",
                mensagem = "Apenas o dono do estabelecimento pode acessar a assinatura."
            });
        }

        var dto = await _requestBus.Query<ObterMinhaAssinaturaQuery, AssinaturaDto?>(
            new ObterMinhaAssinaturaQuery { EstabelecimentoId = tenant.EstabelecimentoId });

        if (dto is null)
            return NotFound(new { mensagem = "Assinatura não encontrada para este estabelecimento." });

        return Ok(dto);
    }

    /// <summary>Lista os planos ativos do catálogo (uso pelo seletor de plano do frontend).</summary>
    [HttpGet("/api/planos")]
    [ProducesResponseType(typeof(IEnumerable<PlanoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPlanos()
    {
        var planos = await _requestBus.Query<ListarPlanosQuery, IEnumerable<PlanoDto>>(new ListarPlanosQuery());
        return Ok(planos);
    }
}
