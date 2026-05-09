using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Contracts.Lgpd.Queries;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints de "minha conta" do titular autenticado.
/// Implementa os direitos LGPD de portabilidade (GET) e exclusão/esquecimento (DELETE).
/// </summary>
[Authorize]
[ApiController]
[Route("api/minha-conta")]
[Produces("application/json")]
public class MinhaContaController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly IMemoryCache _cache;

    public MinhaContaController(ICommandBus commandBus, IRequestBus requestBus, IMemoryCache cache)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _cache = cache;
    }

    /// <summary>
    /// Exporta os dados de conta do titular (Art. 18 LGPD — direito de portabilidade).
    ///
    /// Retorna dados de conta, vínculos, notificações e consentimentos.
    /// Dados clínicos profundos (prontuário, receitas) são omitidos nesta versão — TODO 4.3-V2.
    /// </summary>
    [HttpGet("exportar-dados")]
    [ProducesResponseType(typeof(MeusDadosLgpdDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportarDados()
    {
        var dados = await _requestBus.Query<ExportarMeusDadosQuery, MeusDadosLgpdDto>(
            new ExportarMeusDadosQuery { UsuarioId = ObterUsuarioId() });
        return Ok(dados);
    }

    /// <summary>
    /// Anonimiza a conta do titular (Art. 18 LGPD — direito ao esquecimento).
    ///
    /// Não realiza exclusão física: substitui PII por valores neutros e registra em audit.
    /// O frontend deve chamar logout (revoga refresh token) e redirecionar para /login.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnonimizarConta()
    {
        var userId = ObterUsuarioId();

        await _commandBus.Send(new AnonimizarMinhaContaCommand
        {
            UsuarioId = userId
        });

        // Invalida o cache de /auth/me — nome/telefone foram zerados pela anonimização.
        _cache.Remove(AuthController.AuthMeCacheKey(userId));

        // 204 sem corpo. O frontend interpreta este status como sinal para chamar logout
        // (revoga refresh token) e redirecionar para /login — o access token continua válido
        // até expirar ou ser revogado.
        return NoContent();
    }

    private Guid ObterUsuarioId() => Guid.Parse(User.FindFirst("sub")!.Value);
}
