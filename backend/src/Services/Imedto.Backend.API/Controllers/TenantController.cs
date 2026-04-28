using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoint utilitário que retorna o contexto de tenant atual. Serve para o frontend
/// confirmar qual estabelecimento está ativo e o papel do usuário nele, além de ser
/// o menor teste possível do <c>RequiresEstabelecimentoAttribute</c>.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Route("api/tenant")]
[Produces("application/json")]
public class TenantController : ControllerBase
{
    private readonly ICurrentTenantAccessor _tenant;

    public TenantController(ICurrentTenantAccessor tenant)
    {
        _tenant = tenant;
    }

    /// <summary>Retorna o tenant ativo (exige header <c>X-Estabelecimento-Id</c> válido).</summary>
    [HttpGet("contexto")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Contexto() => Ok(new
    {
        estabelecimentoId = _tenant.EstabelecimentoId,
        usuarioId = _tenant.UsuarioId,
        papel = _tenant.Papel
    });
}
