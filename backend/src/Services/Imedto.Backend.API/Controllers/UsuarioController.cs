using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Contracts.Usuarios.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;

namespace Imedto.Backend.API.Controllers;

/// <summary>Gerenciamento do próprio perfil do usuário autenticado.</summary>
[Authorize]
[ApiController]
[Route("api/usuario")]
[Produces("application/json")]
public class UsuarioController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly IMemoryCache _cache;

    public UsuarioController(ICommandBus commandBus, IRequestBus requestBus, IMemoryCache cache)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _cache = cache;
    }

    /// <summary>Atualização parcial do próprio perfil (nome, telefone).</summary>
    /// <response code="204">Perfil atualizado.</response>
    [HttpPatch("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new AtualizarPerfilUsuarioCommand
        {
            UsuarioId = userId,
            NomeCompleto = request.NomeCompleto,
            Telefone = request.Telefone
        });

        // Invalida o cache de /auth/me — nome e telefone mudaram.
        _cache.Remove(AuthController.AuthMeCacheKey(userId));

        return NoContent();
    }

    /// <summary>Finaliza o onboarding (preenche nome, CPF e marca usuário como ativo).</summary>
    /// <response code="204">Onboarding concluído.</response>
    /// <response code="422">CPF já cadastrado em outra conta ou dados inválidos.</response>
    [AllowBeforeOnboarding]
    [HttpPost("me/onboarding")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompletarOnboarding([FromBody] OnboardingRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new CompletarOnboardingUsuarioCommand
        {
            UsuarioId = userId,
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            Telefone = request.Telefone
        });

        // Invalida cache do filtro de onboarding para que as próximas chamadas
        // (criar estabelecimento, salvar profissional, etc.) passem imediatamente.
        _cache.Remove($"onboarding:{userId}");
        // E também o cache de /auth/me — onboardingCompleto, nome e telefone mudaram.
        _cache.Remove(AuthController.AuthMeCacheKey(userId));

        return NoContent();
    }

    /// <summary>
    /// Verifica se um CPF é válido (algoritmo padrão) e está disponível
    /// (não cadastrado em outra conta). Usado pelo onboarding para feedback inline.
    /// </summary>
    /// <remarks>
    /// Rate-limited (auth-sensitive: 3 req/min por IP) — endpoint sensivel a
    /// enumeracao: sem o limite seria possivel descobrir CPFs cadastrados na
    /// base inteira em poucas horas.
    /// </remarks>
    /// <response code="200">Resultado da verificação.</response>
    /// <response code="429">Muitas tentativas — aguarde 60s.</response>
    [AllowBeforeOnboarding]
    [HttpGet("me/cpf-disponivel")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<VerificarCpfDisponivelResult>> VerificarCpfDisponivel(
        [FromQuery] string cpf)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await _requestBus.Query<VerificarCpfDisponivelQuery, VerificarCpfDisponivelResult>(
            new VerificarCpfDisponivelQuery { UsuarioId = userId, Cpf = cpf ?? "" });
        return Ok(result);
    }
}

public record AtualizarPerfilRequest(string NomeCompleto, string Telefone);
public record OnboardingRequest(string NomeCompleto, string Cpf, string Telefone);
