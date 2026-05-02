using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Autenticação via BFF (Backend for Frontend).
/// O frontend nunca recebe nem armazena tokens — ficam em cookies HttpOnly.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICommandBus _commandBus;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IAuthService authService,
        ICommandBus commandBus,
        IUsuarioRepository usuarioRepository,
        IWebHostEnvironment env)
    {
        _authService = authService;
        _commandBus = commandBus;
        _usuarioRepository = usuarioRepository;
        _env = env;
    }

    /// <summary>Cria uma nova conta (signup no Supabase + registro local).</summary>
    /// <remarks>
    /// Se o projeto Supabase exigir confirmação de e-mail, a resposta retorna
    /// `requerConfirmacaoEmail: true` e NÃO seta cookies — o frontend deve exibir
    /// mensagem para o usuário confirmar o e-mail antes de logar.
    /// </remarks>
    /// <response code="201">Conta criada com sucesso.</response>
    /// <response code="422">Dados inválidos (e-mail já existe, senha fraca, etc.).</response>
    [HttpPost("signup")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        var signup = await _authService.SignupAsync(request.Email, request.Password);

        // Cria o registro de domínio — mesmo se a confirmação de e-mail for obrigatória,
        // já refletimos o usuário em public.usuarios para simplificar o fluxo pós-confirmação.
        await _commandBus.Send(new CriarRegistroLocalUsuarioCommand
        {
            Id = Guid.Parse(signup.User.Id),
            Email = signup.User.Email
        });

        if (signup.Session is null)
        {
            return StatusCode(StatusCodes.Status201Created, new
            {
                usuario = signup.User,
                requerConfirmacaoEmail = true
            });
        }

        SetAuthCookies(signup.Session);

        var payload = _env.IsDevelopment()
            ? new { usuario = signup.User, accessToken = signup.Session.AccessToken, requerConfirmacaoEmail = false }
            : (object)new { usuario = signup.User, requerConfirmacaoEmail = false };

        return StatusCode(StatusCodes.Status201Created, payload);
    }

    /// <summary>Autentica o usuário e seta os cookies HttpOnly de sessão.</summary>
    /// <remarks>
    /// Em desenvolvimento, retorna também o `accessToken` para facilitar testes via Swagger.
    /// </remarks>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="422">Credenciais inválidas.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        SetAuthCookies(result);

        // Atualiza último acesso localmente (idempotente — cria o registro se não existir).
        await _commandBus.Send(new CriarRegistroLocalUsuarioCommand
        {
            Id = Guid.Parse(result.User.Id),
            Email = result.User.Email
        });

        if (_env.IsDevelopment())
            return Ok(new { usuario = result.User, accessToken = result.AccessToken });

        return Ok(new { usuario = result.User });
    }

    /// <summary>Renova o access token usando o refresh token (cookie automático).</summary>
    /// <response code="200">Token renovado com sucesso.</response>
    /// <response code="401">Sessão não encontrada ou refresh token expirado.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refresh-token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { mensagem = "Sessão não encontrada." });

        var result = await _authService.RefreshAsync(refreshToken);
        SetAuthCookies(result);

        // Retorna o usuário de domínio (igual ao /auth/me) para que o frontend
        // receba onboardingCompleto e demais campos locais corretamente.
        if (!Guid.TryParse(result.User.Id, out var userId))
            return Unauthorized();

        var usuario = await _usuarioRepository.ObterPorIdOuNulo(userId);
        if (usuario is null)
            return Unauthorized(new { mensagem = "Usuário não encontrado." });

        // Payload minimizado (LGPD): cpf removido (nao usado pelo front) e
        // ultimoAcessoEm removido (sem uso). Telefone mantido — eh round-trip
        // do form em MinhaContaView (front precisa do valor existente para editar).
        return Ok(new
        {
            usuario = new
            {
                id = usuario.Id,
                email = usuario.Email,
                nomeCompleto = usuario.NomeCompleto,
                telefone = usuario.Telefone,
                status = usuario.Status.ToString(),
                onboardingCompleto = usuario.OnboardingCompleto
            }
        });
    }

    /// <summary>Encerra a sessão, invalida o token no Supabase e limpa os cookies.</summary>
    /// <response code="204">Logout realizado com sucesso.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var accessToken = Request.Cookies["access-token"];
        if (!string.IsNullOrEmpty(accessToken))
            await _authService.LogoutAsync(accessToken);

        ClearAuthCookies();
        return NoContent();
    }

    /// <summary>Retorna os dados agregados do usuário autenticado (Supabase + registro local).</summary>
    /// <remarks>Chamado pelo frontend no carregamento da app para reidratar a sessão.</remarks>
    /// <response code="200">Usuário autenticado.</response>
    /// <response code="401">Não autenticado.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var usuario = await _usuarioRepository.ObterPorIdOuNulo(userId);
        if (usuario is null)
            throw new BusinessException("Registro local do usuário não encontrado.");

        // Payload minimizado (LGPD): cpf removido (nao usado pelo front) e
        // ultimoAcessoEm removido (sem uso). Telefone mantido — eh round-trip
        // do form em MinhaContaView (front precisa do valor existente para editar).
        return Ok(new
        {
            usuario = new
            {
                id = usuario.Id,
                email = usuario.Email,
                nomeCompleto = usuario.NomeCompleto,
                telefone = usuario.Telefone,
                status = usuario.Status.ToString(),
                onboardingCompleto = usuario.OnboardingCompleto
            }
        });
    }

    // ---- Helpers ----

    /// <summary>Envia e-mail de recuperação de senha.</summary>
    /// <remarks>Sempre retorna 204 independente de o e-mail existir (prevenção de enumeração).</remarks>
    /// <response code="204">E-mail enviado (ou silenciado).</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return NoContent();

        var redirectTo = $"{Request.Scheme}://{Request.Host}/reset-password";
        await _authService.EnviarRecuperacaoSenhaAsync(request.Email, redirectTo);
        return NoContent();
    }

    private void SetAuthCookies(AuthResult result)
    {
        var isDev = _env.IsDevelopment();
        var secure = !isDev;
        // Em dev (same-origin via proxy do Vite) usamos Lax. Em prod o front
        // pode estar em domínio diferente do backend (Vercel ↔ Render), então
        // o cookie precisa ser `SameSite=None` — exige Secure (HTTPS), o que
        // já está garantido em produção pelo proxy TLS do Render.
        var sameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None;

        // Item 4.8 — access-token usa Path="/" para que o cookie seja enviado tanto em
        // /api/* quanto em /hubs/* (SignalR). Continua HttpOnly + Secure + SameSite=Strict
        // (em prod), então ampliar o escopo de envio não introduz risco a XSS/CSRF — apenas
        // permite o handshake do SignalR autenticar pelo cookie em vez de query string.
        Response.Cookies.Append("access-token", result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = result.ExpiresAt,
            Path = "/"
        });

        // Refresh-token continua restrito ao endpoint que o consome — princípio do
        // menor privilégio: o cookie de refresh nunca trafega em /api/* nem /hubs/*.
        Response.Cookies.Append("refresh-token", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/api/auth/refresh"
        });
    }

    private void ClearAuthCookies()
    {
        // Path precisa casar com o usado em SetAuthCookies — caso contrário o browser
        // não remove o cookie e a sessão "vaza" entre logouts.
        Response.Cookies.Delete("access-token", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("refresh-token", new CookieOptions { Path = "/api/auth/refresh" });
    }
}

/// <summary>Payload de login.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Payload de signup.</summary>
public record SignupRequest(string Email, string Password);

/// <summary>Payload de recuperação de senha.</summary>
public record ForgotPasswordRequest(string Email);
