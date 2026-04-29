using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints administrativos internos. Acesso restrito — não expor ao público.
///
/// Segurança atual: disponível apenas em Development. Em produção, bloquear via
/// network policy ou API gateway antes de habilitar claim <c>imedto_admin</c>.
///
/// TODO Wave futura: validar claim <c>imedto_admin = true</c> no JWT (requer
/// configuração no Supabase Auth com custom claims via hook JWT).
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminResetService _resetService;
    private readonly IWebHostEnvironment _env;

    public AdminController(IAdminResetService resetService, IWebHostEnvironment env)
    {
        _resetService = resetService;
        _env = env;
    }

    /// <summary>
    /// Remove todo o conteúdo de um estabelecimento (hard delete bypass do SoftDeleteInterceptor).
    /// Mantém a casca (registro em <c>estabelecimentos</c>). Registra auditoria obrigatória.
    ///
    /// Disponível apenas em Development até que o mecanismo de claim <c>imedto_admin</c>
    /// seja implementado no Supabase Auth.
    /// </summary>
    /// <response code="204">Reset concluído com sucesso.</response>
    /// <response code="422">Parâmetros inválidos ou ambiente não autorizado.</response>
    [HttpPost("estabelecimentos/{id:long}/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetEstabelecimento(
        long id,
        [FromBody] AdminResetRequest request,
        CancellationToken ct)
    {
        // TODO Wave futura: substituir por validação de claim imedto_admin = true no JWT.
        // Enquanto o mecanismo não existe, o endpoint só funciona em Development.
        if (!_env.IsDevelopment())
            throw new SharedKernel.Domain.BusinessException(
                "Endpoint admin requer claim imedto_admin (não implementado em produção).");

        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _resetService.ResetEstabelecimentoAsync(id, request.Motivo, userId, ct);

        return NoContent();
    }
}

public record AdminResetRequest(string Motivo);
