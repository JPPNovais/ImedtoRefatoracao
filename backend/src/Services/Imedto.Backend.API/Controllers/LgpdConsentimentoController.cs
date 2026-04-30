using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Contracts.Lgpd.Queries;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Registra e consulta consentimentos LGPD do titular autenticado.
/// O ip/userAgent são capturados server-side — o frontend não os envia.
/// </summary>
[Authorize]
[ApiController]
[Route("api/lgpd/consentimentos")]
[Produces("application/json")]
public class LgpdConsentimentoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public LgpdConsentimentoController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>Registra o aceite de um documento LGPD (termos, política, etc.).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarConsentimentoBody body)
    {
        await _commandBus.Send(new RegistrarConsentimentoCommand
        {
            UsuarioId = ObterUsuarioId(),
            Tipo = body.Tipo,     // string — handler faz Enum.TryParse
            Versao = body.Versao,
            IpOrigem = ObterIpOrigem(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });
        return NoContent();
    }

    /// <summary>Lista todos os consentimentos registrados pelo titular.</summary>
    [HttpGet("meus")]
    [ProducesResponseType(typeof(IEnumerable<ConsentimentoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMeus()
    {
        var resultado = await _requestBus.Query<ListarMeusConsentimentosQuery, IEnumerable<ConsentimentoDto>>(
            new ListarMeusConsentimentosQuery { UsuarioId = ObterUsuarioId() });
        return Ok(resultado);
    }

    private Guid ObterUsuarioId() => Guid.Parse(User.FindFirst("sub")!.Value);

    private string ObterIpOrigem()
    {
        // Respeita proxies/load balancers via X-Forwarded-For; fallback para conexão direta.
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>Valores válidos para Tipo: TermosUso, PoliticaPrivacidade, UsoIA, UsoDadosClinicos.</summary>
public sealed record RegistrarConsentimentoBody(string Tipo, string Versao);
