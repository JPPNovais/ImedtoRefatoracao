using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints anônimos do fluxo de aceite/recusa via link público.
///
/// <para><b>Fase 1: stubs</b> — registramos as rotas para o frontend já consumir, mas
/// retornam 501 NotImplemented. Fluxo completo (token validation, rate limit, aceite,
/// notificação por e-mail) será implementado na Fase 4.</para>
///
/// <para>Quando implementados, estes endpoints serão <c>[AllowAnonymous]</c> e ficarão
/// atrás de rate limiter agressivo (por IP) para evitar enumeração de tokens.</para>
/// </summary>
[AllowAnonymous]
[ApiController]
[Produces("application/json")]
public class TermoPublicoController : ControllerBase
{
    /// <summary>Stub — visualizar conteúdo do termo via token. Fase 4.</summary>
    [HttpGet("api/publico/termos/aceite/{token}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Visualizar(string token) =>
        StatusCode(StatusCodes.Status501NotImplemented, new
        {
            tipo = "NaoImplementado",
            mensagem = "Fluxo de aceite público planejado para a Fase 4.",
        });

    /// <summary>Stub — registrar aceite/recusa via token. Fase 4.</summary>
    [HttpPost("api/publico/termos/aceite/{token}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RegistrarResposta(string token, [FromBody] RegistrarRespostaRequest request) =>
        StatusCode(StatusCodes.Status501NotImplemented, new
        {
            tipo = "NaoImplementado",
            mensagem = "Fluxo de aceite público planejado para a Fase 4.",
        });
}

public record RegistrarRespostaRequest(bool Aceito, string NomeConfirmado);
