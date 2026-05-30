using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Imedto.Backend.Contracts.Admin.Auth;
using Imedto.Backend.Contracts.Admin.Auth.Results;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Auth dedicada para administradores globais do Imedto.
///
/// BFF pattern: tokens ficam em cookies HttpOnly — frontend nunca vê nem armazena.
/// Access token: 15min (curto por ser privilégio global).
/// Refresh token: 2h, rotacionado a cada uso.
/// Claim JWT: <c>imedto_admin = "true"</c>. Nunca carrega <c>estabelecimento_id</c>.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
[Produces("application/json")]
public class AdminAuthController : ControllerBase
{
    private readonly ImedtoAdminTokenIssuer _tokenIssuer;
    private readonly ImedtoAdminRepository _adminRepo;
    private readonly ImedtoAdminRefreshTokenRepository _refreshRepo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly IPasswordHasher _hasher;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    public AdminAuthController(
        ImedtoAdminTokenIssuer tokenIssuer,
        ImedtoAdminRepository adminRepo,
        ImedtoAdminRefreshTokenRepository refreshRepo,
        ImedtoAdminAuditWriter audit,
        IPasswordHasher hasher,
        IWebHostEnvironment env,
        AppDbContext db)
    {
        _tokenIssuer = tokenIssuer;
        _adminRepo = adminRepo;
        _refreshRepo = refreshRepo;
        _audit = audit;
        _hasher = hasher;
        _env = env;
        _db = db;
    }

    /// <summary>Autentica administrador e seta cookies HttpOnly de sessão.</summary>
    /// <response code="200">Login realizado. Cookie admin-access-token setado.</response>
    /// <response code="401">Credenciais inválidas (mensagem genérica).</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AdminLoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            return Unauthorized(new { mensagem = "Credenciais inválidas." });

        var emailNorm = request.Email.Trim().ToLowerInvariant();
        var admin = await _adminRepo.ObterPorEmailAsync(emailNorm, ct);

        // Timing equalizado: sempre executa hash para evitar timing oracle.
        const string DummyHash = "$2a$12$DummyDummyDummyDummyDummyDummyDummyDummyDummyDummyDu";
        bool senhaCorreta;
        if (admin is not null)
            senhaCorreta = _hasher.Verificar(request.Senha, admin.SenhaHash);
        else
        {
            _ = _hasher.Verificar(request.Senha, DummyHash);
            senhaCorreta = false;
        }

        if (admin is null || !admin.Ativo || !senhaCorreta)
        {
            // Audit LOGIN_FAIL — admin pode não existir, adminId fica null. Não logar email.
            await _audit.RegistrarAsync(
                AcoesAuditAdmin.LoginFail,
                adminId: null,
                recursoTipo: "admin",
                motivo: "credencial_invalida",
                ct: ct);
            return Unauthorized(new { mensagem = "Credenciais inválidas." });
        }

        // Login válido — registrar acesso e emitir tokens.
        admin.RegistrarLogin();
        _adminRepo.Atualizar(admin);

        var accessToken = _tokenIssuer.EmitirAccessToken(admin, admin.ForcePasswordReset);
        var (refreshCru, refreshHash, refreshExpira) = ImedtoAdminTokenIssuer.GerarRefreshToken();

        var ip = ObterIp();
        var ua = Request.Headers.UserAgent.ToString();

        _refreshRepo.Adicionar(ImedtoAdminRefreshToken.Criar(admin.Id, refreshHash, refreshExpira, ip, ua));
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.LoginOk,
            adminId: admin.Id,
            recursoTipo: "admin",
            recursoId: admin.Id.ToString(),
            ct: ct);

        SetarCookies(accessToken.Token, refreshCru, refreshExpira);

        var result = new AdminLoginResult(
            admin.Id,
            admin.Email,
            admin.Nome,
            admin.ForcePasswordReset);

        // Em dev: retorna accessToken no body para testes via Swagger/curl.
        if (_env.IsDevelopment())
            return Ok(new { admin = result, accessToken = accessToken.Token });

        return Ok(new { admin = result });
    }

    /// <summary>Renova access token usando refresh token (cookie automático). Rotaciona o refresh.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshCru = Request.Cookies["admin-refresh-token"];
        if (string.IsNullOrEmpty(refreshCru))
            return Unauthorized(new { mensagem = "Sessão expirada. Faça login novamente." });

        var hash = ImedtoAdminTokenIssuer.HashToken(refreshCru);
        var token = await _refreshRepo.ObterPorHashAsync(hash, ct);

        if (token is null || !token.EstaAtivo())
        {
            LimparCookies();
            return Unauthorized(new { mensagem = "Sessão expirada. Faça login novamente." });
        }

        var admin = await _adminRepo.ObterPorIdAsync(token.AdminId, ct);
        if (admin is null || !admin.Ativo)
        {
            LimparCookies();
            return Unauthorized(new { mensagem = "Sessão expirada. Faça login novamente." });
        }

        // Rotação: revoga o antigo, emite novo.
        token.Revogar();
        _refreshRepo.Atualizar(token);

        var novoAccess = _tokenIssuer.EmitirAccessToken(admin, admin.ForcePasswordReset);
        var (novoCru, novoHash, novaExpira) = ImedtoAdminTokenIssuer.GerarRefreshToken();

        var ip = ObterIp();
        var ua = Request.Headers.UserAgent.ToString();
        _refreshRepo.Adicionar(ImedtoAdminRefreshToken.Criar(admin.Id, novoHash, novaExpira, ip, ua));
        await _db.SaveChangesAsync(ct);

        SetarCookies(novoAccess.Token, novoCru, novaExpira);
        return Ok(new { ok = true });
    }

    /// <summary>Encerra sessão: limpa cookies e revoga refresh token.</summary>
    [HttpPost("logout")]
    [Authorize(Policy = "ImedtoAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var adminId = ObterAdminId();

        var refreshCru = Request.Cookies["admin-refresh-token"];
        if (!string.IsNullOrEmpty(refreshCru))
        {
            var hash = ImedtoAdminTokenIssuer.HashToken(refreshCru);
            var token = await _refreshRepo.ObterPorHashAsync(hash, ct);
            if (token is not null)
            {
                token.Revogar();
                _refreshRepo.Atualizar(token);
                await _db.SaveChangesAsync(ct);
            }
        }

        if (adminId.HasValue)
        {
            await _audit.RegistrarAsync(
                AcoesAuditAdmin.Logout,
                adminId: adminId,
                recursoTipo: "admin",
                recursoId: adminId.ToString(),
                ct: ct);
        }

        LimparCookies();
        return Ok(new { ok = true });
    }

    /// <summary>Retorna dados do admin autenticado.</summary>
    [HttpGet("me")]
    [Authorize(Policy = "ImedtoAdmin")]
    [ProducesResponseType(typeof(AdminMeResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        var admin = await _adminRepo.ObterPorIdAsync(adminId.Value, ct);
        if (admin is null || !admin.Ativo) return Unauthorized();

        return Ok(new AdminMeResult(
            admin.Id, admin.Email, admin.Nome, admin.Ativo,
            admin.ForcePasswordReset, admin.UltimoLoginEm));
    }

    /// <summary>
    /// Altera a própria senha. Aceita tokens com <c>must_reset_password = true</c>
    /// (policy <c>ImedtoAdminChangePassword</c> — mais permissiva que <c>ImedtoAdmin</c>).
    /// </summary>
    [HttpPost("change-password")]
    [Authorize(Policy = "ImedtoAdminChangePassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] AdminChangePasswordRequest request, CancellationToken ct)
    {
        var adminId = ObterAdminId();
        if (!adminId.HasValue) return Unauthorized();

        var admin = await _adminRepo.ObterPorIdAsync(adminId.Value, ct);
        if (admin is null || !admin.Ativo) return Unauthorized();

        // Valida política de senha (lança BusinessException → 422 via GlobalExceptionFilter).
        AdminSenhaPolicy.Validar(request.NovaSenha, _env.IsDevelopment());

        var novoHash = _hasher.Hash(request.NovaSenha);
        admin.AtualizarSenha(novoHash, forceReset: false);
        _adminRepo.Atualizar(admin);

        // Revoga todas as sessões antigas e emite nova sessão sem a claim de reset.
        await _refreshRepo.RevogarTodosDoAdminAsync(adminId.Value, ct);

        var novoAccess = _tokenIssuer.EmitirAccessToken(admin, false);
        var (novoCru, novoHash2, novaExpira) = ImedtoAdminTokenIssuer.GerarRefreshToken();

        var ip = ObterIp();
        var ua = Request.Headers.UserAgent.ToString();
        _refreshRepo.Adicionar(ImedtoAdminRefreshToken.Criar(admin.Id, novoHash2, novaExpira, ip, ua));
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.ResetSenhaPropria,
            adminId: adminId,
            recursoTipo: "admin",
            recursoId: adminId.ToString(),
            ct: ct);

        SetarCookies(novoAccess.Token, novoCru, novaExpira);
        return Ok(new { ok = true });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private void SetarCookies(string accessToken, string refreshCru, DateTimeOffset refreshExpira)
    {
        var secure = !_env.IsDevelopment();

        // Caminho do access token: todas as rotas admin.
        Response.Cookies.Append("admin-access-token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/api/admin",
            Expires = DateTimeOffset.UtcNow.AddMinutes(16) // margem de 1min sobre o TTL do JWT
        });

        // Caminho do refresh restrito ao endpoint de refresh — evita envio em toda request.
        Response.Cookies.Append("admin-refresh-token", refreshCru, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/api/admin/auth/refresh",
            Expires = refreshExpira
        });
    }

    private void LimparCookies()
    {
        Response.Cookies.Delete("admin-access-token", new CookieOptions { Path = "/api/admin" });
        Response.Cookies.Delete("admin-refresh-token", new CookieOptions { Path = "/api/admin/auth/refresh" });
    }

    private Guid? ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? ObterIp()
    {
        var fwd = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
            return fwd.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
