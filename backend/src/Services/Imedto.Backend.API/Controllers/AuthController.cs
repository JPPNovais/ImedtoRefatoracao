using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Imedto.Backend.Contracts.Auth.Queries;
using Imedto.Backend.Contracts.Auth.Queries.Results;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Filters;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Autenticação via BFF (Backend for Frontend).
/// O frontend nunca recebe nem armazena tokens — ficam em cookies HttpOnly.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[AllowBeforeOnboarding]
public class AuthController : ControllerBase
{
    /// <summary>
    /// TTL do cache de /auth/me. Curto o suficiente para que mudanças em nome/telefone/onboarding
    /// (já invalidadas pelas controllers que escrevem) só fiquem stale por no máximo 60s mesmo
    /// que a invalidação por chave falhe ou que a edição venha de outro nó futuramente.
    /// </summary>
    private static readonly TimeSpan AuthMeCacheTtl = TimeSpan.FromSeconds(60);

    internal static string AuthMeCacheKey(Guid usuarioId) => $"auth:me:{usuarioId}";

    private readonly IAuthService _authService;
    private readonly LocalJwtAuthService _localAuth;
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IAuthService authService,
        LocalJwtAuthService localAuth,
        ICommandBus commandBus,
        IRequestBus requestBus,
        IUsuarioRepository usuarioRepository,
        IMemoryCache cache,
        IWebHostEnvironment env)
    {
        _authService = authService;
        _localAuth = localAuth;
        _commandBus = commandBus;
        _requestBus = requestBus;
        _usuarioRepository = usuarioRepository;
        _cache = cache;
        _env = env;
    }

    /// <summary>Cria uma nova conta (credencial de auth + registro local).</summary>
    /// <remarks>
    /// Se a configuração exigir confirmação de e-mail, a resposta retorna
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

        AuthResult result;
        try
        {
            result = await _authService.RefreshAsync(refreshToken);
        }
        catch (BusinessException)
        {
            // Refresh expirado/invalido eh estado normal de fim-de-sessao — devolver
            // 401 (Swagger ja declara) em vez de 422. O interceptor do front depende
            // de 401 para acionar logout/redirect; 422 quebra o ciclo.
            ClearAuthCookies();
            return Unauthorized(new { mensagem = "Sessão expirada. Faça login novamente." });
        }

        SetAuthCookies(result);

        // Retorna o usuário de domínio (igual ao /auth/me) para que o frontend
        // receba onboardingCompleto e demais campos locais corretamente.
        if (!Guid.TryParse(result.User.Id, out var userId))
            return Unauthorized();

        // Refresh recém-emitiu novo token: invalida o cache para que o próximo /me reflita
        // qualquer alteração de domínio (ex: onboarding concluído num nó paralelo).
        _cache.Remove(AuthMeCacheKey(userId));

        var payload = await ObterMePayload(userId);
        if (payload is null)
            return Unauthorized(new { mensagem = "Usuário não encontrado." });

        return Ok(new { usuario = payload });
    }

    /// <summary>Encerra a sessão, revoga o refresh token e limpa os cookies.</summary>
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

    /// <summary>Retorna os dados agregados do usuário autenticado (credencial + registro local).</summary>
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

        var payload = await ObterMePayload(userId);
        if (payload is null)
            throw new BusinessException("Registro local do usuário não encontrado.");

        return Ok(new { usuario = payload });
    }

    /// <summary>
    /// Reidrata todo o estado da SPA em uma única chamada: usuário, cadastro
    /// profissional (quando existir) e estabelecimentos vinculados. Substitui a
    /// sequência /auth/me + /profissional/me + /estabelecimento no boot do front.
    /// </summary>
    /// <response code="200">Estado de auth carregado — body <c>null</c> quando sem sessão.</response>
    [HttpGet("bootstrap")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BootstrapMeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Bootstrap()
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        // Sem sessão: retorna 200 com body null em vez de 401. Esse endpoint roda no
        // boot do SPA — o browser logava "Failed to load resource" em cada carga
        // anônima (landing/login). Como não há vazamento de PII em retornar null,
        // troca pra 200 elimina o ruído nas DevTools e simplifica o front.
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Ok((BootstrapMeDto?)null);

        var dto = await _requestBus.Query<BootstrapMeQuery, BootstrapMeDto>(
            new BootstrapMeQuery { UsuarioId = userId });

        return Ok(dto);
    }

    /// <summary>
    /// Registra o último estabelecimento acessado pelo usuário. Chamado pelo front
    /// ao trocar de estabelecimento manualmente ou ao resolver via fallback no boot.
    /// Falha-fechada: valida vínculo Ativo ou Dono antes de gravar.
    /// </summary>
    /// <response code="204">Último estabelecimento registrado.</response>
    /// <response code="401">Não autenticado.</response>
    /// <response code="422">Estabelecimento não acessível pelo usuário.</response>
    [HttpPost("ultimo-estabelecimento")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegistrarUltimoEstabelecimento(
        [FromBody] RegistrarUltimoEstabelecimentoRequest request)
    {
        await _commandBus.Send(new RegistrarUltimoEstabelecimentoCommand
        {
            EstabelecimentoId = request.EstabelecimentoId,
        });
        return NoContent();
    }

    /// <summary>
    /// Carrega o payload de /auth/me com cache em memória de 60s. O cache é invalidado
    /// pelas controllers que mutam os campos retornados (UsuarioController, OnboardingController,
    /// MinhaContaController) — ver <see cref="AuthMeCacheKey(Guid)"/>.
    /// </summary>
    private async Task<MeUsuarioDto?> ObterMePayload(Guid userId)
    {
        var key = AuthMeCacheKey(userId);
        if (_cache.TryGetValue<MeUsuarioDto>(key, out var cached))
            return cached;

        // AsNoTracking: rota pura de leitura — não queremos pagar o overhead do change tracker.
        var usuario = await _usuarioRepository.ObterPorIdParaLeitura(userId);
        if (usuario is null)
            return null;

        // Payload minimizado (LGPD): cpf removido (nao usado pelo front) e
        // ultimoAcessoEm removido (sem uso). Telefone mantido — eh round-trip
        // do form em MinhaContaView (front precisa do valor existente para editar).
        // UltimoEstabelecimentoId: exposto apenas no bootstrap (nao no /me) — o /me
        // serve para reidratar dados de perfil, nao preferencia de tenant.
        var payload = new MeUsuarioDto(
            usuario.Id,
            usuario.Email,
            usuario.NomeCompleto,
            usuario.Telefone,
            usuario.Status.ToString(),
            usuario.OnboardingCompleto);

        _cache.Set(key, payload, AuthMeCacheTtl);
        return payload;
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

    /// <summary>
    /// Reenvia o e-mail de confirmação para contas pendentes. Sempre retorna 204
    /// (anti-enumeração — não revela se o e-mail existe).
    /// </summary>
    /// <response code="204">Solicitação processada (e-mail enviado se aplicável).</response>
    [HttpPost("reenviar-confirmacao")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReenviarConfirmacao([FromBody] ReenviarConfirmacaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Email)) return NoContent();
        await _localAuth.ReenviarConfirmacaoEmailAsync(request.Email);
        return NoContent();
    }

    /// <summary>Confirma o e-mail consumindo um token recebido por e-mail no signup.</summary>
    /// <response code="204">E-mail confirmado.</response>
    /// <response code="422">Token inválido ou expirado.</response>
    [HttpPost("confirmar-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmarEmail([FromBody] ConfirmarEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            throw new BusinessException("Token é obrigatório.");
        await _localAuth.ConfirmarEmailAsync(request.Token);
        return NoContent();
    }

    /// <summary>Redefine senha consumindo um token de recuperação enviado por e-mail.</summary>
    /// <remarks>Após sucesso, todas as sessões ativas do usuário são revogadas.</remarks>
    /// <response code="204">Senha redefinida.</response>
    /// <response code="422">Token inválido/expirado ou senha não atende aos requisitos.</response>
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            throw new BusinessException("Token é obrigatório.");
        await _localAuth.RedefinirSenhaAsync(request.Token, request.NovaSenha);
        return NoContent();
    }

    /// <summary>
    /// Altera a senha do usuário autenticado. Exige a senha atual (reautenticação)
    /// e revoga todas as sessões ativas após sucesso — o usuário continua logado
    /// nesta sessão (cookies já emitidos) mas precisa re-logar nos demais devices.
    /// </summary>
    /// <response code="204">Senha alterada com sucesso.</response>
    /// <response code="422">Senha atual incorreta, senha nova fraca ou igual à atual.</response>
    [HttpPost("alterar-senha")]
    [Authorize]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (request is null
            || string.IsNullOrEmpty(request.SenhaAtual)
            || string.IsNullOrEmpty(request.NovaSenha))
        {
            throw new BusinessException("Informe senha atual e nova senha.");
        }

        await _authService.AlterarSenhaAsync(userId, request.SenhaAtual, request.NovaSenha);
        return NoContent();
    }

    /// <summary>
    /// Aceita um convite de profissional: define senha, confirma e-mail, cria registro
    /// local de usuário e loga automaticamente (seta cookies HttpOnly).
    /// </summary>
    /// <response code="200">Convite aceito + usuário logado.</response>
    /// <response code="422">Token inválido/expirado ou senha fraca.</response>
    [HttpPost("aceitar-convite")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AceitarConvite([FromBody] AceitarConviteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
            throw new BusinessException("Token é obrigatório.");

        var usuarioId = await _localAuth.AceitarConviteAsync(request.Token, request.NovaSenha);

        // Cria/atualiza registro local de usuário (idempotente).
        await _commandBus.Send(new CriarRegistroLocalUsuarioCommand
        {
            Id = usuarioId,
            Email = request.Email
        });

        // Aceita automaticamente o(s) vínculo(s) pendente(s) do convidado.
        // O usuário veio do link do e-mail justamente para se juntar a um
        // estabelecimento — forçá-lo a abrir /meus-convites e clicar
        // "Aceitar" depois de já ter definido senha era um furo de UX e
        // contradizia a mensagem de sucesso ("Conta criada e convite aceito").
        // Idempotente: se não houver pendentes, é no-op.
        await _commandBus.Send(new Imedto.Backend.Contracts.Vinculos.Commands.AceitarConvitesPendentesDoUsuarioCommand
        {
            ProfissionalUsuarioId = usuarioId
        });

        // Logs in via fluxo padrão (gera novo par access+refresh).
        var auth = await _authService.LoginAsync(request.Email, request.NovaSenha);
        SetAuthCookies(auth);

        var payload = _env.IsDevelopment()
            ? new { usuario = auth.User, accessToken = auth.AccessToken }
            : (object)new { usuario = auth.User };

        return Ok(payload);
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

/// <summary>Payload de reenvio de e-mail de confirmação.</summary>
public record ReenviarConfirmacaoRequest(string Email);

/// <summary>Payload de confirmação de e-mail.</summary>
public record ConfirmarEmailRequest(string Token);

/// <summary>Payload de redefinição de senha (após receber link por e-mail).</summary>
public record RedefinirSenhaRequest(string Token, string NovaSenha);

/// <summary>Payload de aceite de convite (define senha + ativa cadastro).</summary>
public record AceitarConviteRequest(string Token, string Email, string NovaSenha);

/// <summary>Payload de alteração de senha do usuário autenticado.</summary>
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

/// <summary>Payload para registrar o último estabelecimento acessado.</summary>
public record RegistrarUltimoEstabelecimentoRequest(long EstabelecimentoId);
