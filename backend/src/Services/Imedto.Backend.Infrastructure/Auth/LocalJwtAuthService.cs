using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// Implementação 100% local do <see cref="IAuthService"/>. Substitui
/// <c>SupabaseAuthService</c> — sem dependência de provedor externo de auth.
///
/// Persistência: 3 tabelas (auth_credenciais, auth_refresh_tokens, auth_email_tokens).
/// Hashing: BCrypt + pepper (<see cref="IPasswordHasher"/>).
/// JWT: ECDSA P-256 (<see cref="IJwtTokenIssuer"/>).
/// E-mail: provedor injetado (Resend em prod, NoOp em dev).
/// </summary>
public class LocalJwtAuthService : IAuthService
{
    private const int SenhaMinima = 8;
    private static readonly TimeSpan TtlConfirmacao = TimeSpan.FromHours(24);
    private static readonly TimeSpan TtlReset = TimeSpan.FromHours(1);
    private static readonly TimeSpan TtlConvite = TimeSpan.FromDays(7);

    private readonly IAuthCredencialRepository _credenciaisRepo;
    private readonly IAuthRefreshTokenRepository _refreshRepo;
    private readonly IAuthEmailTokenRepository _emailTokenRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenIssuer _issuer;
    private readonly IEmailService _emails;
    private readonly EmailOptions _emailOptions;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<LocalJwtAuthService> _logger;

    public LocalJwtAuthService(
        IAuthCredencialRepository credenciaisRepo,
        IAuthRefreshTokenRepository refreshRepo,
        IAuthEmailTokenRepository emailTokenRepo,
        IPasswordHasher hasher,
        IJwtTokenIssuer issuer,
        IEmailService emails,
        IOptions<EmailOptions> emailOptions,
        IHttpContextAccessor http,
        ILogger<LocalJwtAuthService> logger)
    {
        _credenciaisRepo = credenciaisRepo;
        _refreshRepo = refreshRepo;
        _emailTokenRepo = emailTokenRepo;
        _hasher = hasher;
        _issuer = issuer;
        _emails = emails;
        _emailOptions = emailOptions.Value;
        _http = http;
        _logger = logger;
    }

    public async Task<SignupResult> SignupAsync(string email, string password)
    {
        ValidarEmail(email);
        ValidarSenha(password);

        var emailNorm = email.Trim().ToLowerInvariant();
        if (await _credenciaisRepo.ExisteParaEmailAsync(emailNorm))
            throw new BusinessException("Já existe uma conta com este e-mail.");

        var id = Guid.NewGuid();
        var hash = _hasher.Hash(password);
        var credencial = AuthCredencial.Criar(id, emailNorm, hash);
        await _credenciaisRepo.AdicionarAsync(credencial);

        var (cru, hashTok) = GerarTokenAleatorio();
        var emailTok = AuthEmailToken.Emitir(
            id, AuthEmailTokenTipo.ConfirmacaoEmail, hashTok, DateTime.UtcNow.Add(TtlConfirmacao));
        await _emailTokenRepo.AdicionarAsync(emailTok);

        var link = MontarLink("/auth/confirmar-email", cru);
        await _emails.EnviarAsync(emailNorm, "Confirme seu e-mail no Imedto", TemplatesEmail.Confirmacao(link));

        return new SignupResult(
            new UserInfo(id.ToString(), emailNorm, Array.Empty<string>()),
            null);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        ValidarEmail(email);
        if (string.IsNullOrEmpty(password))
            throw new BusinessException("Credenciais inválidas.");

        var emailNorm = email.Trim().ToLowerInvariant();
        var credencial = await _credenciaisRepo.ObterPorEmailAsync(emailNorm);

        if (credencial is null || !credencial.TemSenhaDefinida)
        {
            // Constant-ish time: aplica hash dummy pra reduzir oráculo de timing.
            _ = _hasher.Verificar(password, "$2a$12$DummyDummyDummyDummyDummyDummyDummyDummyDummyDummyDu");
            _logger.LogWarning("Login falhou (conta inexistente) para {Hash}.", HashEmail(emailNorm));
            throw new BusinessException("Credenciais inválidas.");
        }

        if (credencial.Bloqueado)
        {
            _logger.LogWarning("Login bloqueado para {Hash}: {Motivo}", HashEmail(emailNorm), credencial.MotivoBloqueio);
            throw new BusinessException("Conta bloqueada. Entre em contato com o suporte.");
        }

        if (!_hasher.Verificar(password, credencial.SenhaHash))
        {
            credencial.RegistrarFalhaLogin();
            _credenciaisRepo.Atualizar(credencial);
            _logger.LogWarning("Login falhou (senha errada) para {Hash} — tentativas: {Qtd}.",
                HashEmail(emailNorm), credencial.TentativasFalhas);
            throw new BusinessException("Credenciais inválidas.");
        }

        if (!credencial.EmailConfirmado)
            throw new BusinessException("Confirme seu e-mail antes de entrar.");

        credencial.RegistrarLoginBemSucedido();
        _credenciaisRepo.Atualizar(credencial);

        return await EmitirSessaoAsync(credencial);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BusinessException("Sessão expirada. Faça login novamente.");

        var hash = EcdsaJwtTokenIssuer.HashRefreshToken(refreshToken);
        var existente = await _refreshRepo.ObterPorHashAsync(hash);

        if (existente is null || !existente.Valido)
            throw new BusinessException("Sessão expirada. Faça login novamente.");

        // Rotação: revoga o atual e emite novo (defesa contra replay).
        existente.Revogar();
        _refreshRepo.Atualizar(existente);

        var credencial = await _credenciaisRepo.ObterPorIdAsync(existente.UsuarioId);
        if (credencial is null || credencial.Bloqueado)
            throw new BusinessException("Sessão expirada. Faça login novamente.");

        return await EmitirSessaoAsync(credencial);
    }

    public Task LogoutAsync(string accessToken)
    {
        // Sem blacklist de access tokens (TTL curto + custo alto). O cookie já é
        // apagado pelo controller, e o refresh associado pode ser revogado se
        // tivéssemos rastro dele a partir do access — não temos. Fire-and-forget.
        return Task.CompletedTask;
    }

    public Task<UserInfo> GetUserAsync(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new BusinessException("Token inválido.");

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            var sub = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var emailClaim = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var roles = token.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray();

            if (string.IsNullOrEmpty(sub))
                throw new BusinessException("Token inválido.");

            return Task.FromResult(new UserInfo(sub, emailClaim ?? string.Empty, roles));
        }
        catch (BusinessException) { throw; }
        catch (Exception)
        {
            throw new BusinessException("Token inválido.");
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var id))
            throw new BusinessException("Identificador inválido.");

        var credencial = await _credenciaisRepo.ObterPorIdAsync(id);
        if (credencial is null) return;

        // Cascade na FK do refresh/email_tokens limpa o resto.
        _credenciaisRepo.Remover(credencial);
    }

    public async Task<ConviteResult> CriarConviteAsync(string email)
    {
        ValidarEmail(email);
        var emailNorm = email.Trim().ToLowerInvariant();

        var existente = await _credenciaisRepo.ObterPorEmailAsync(emailNorm);
        if (existente is not null)
        {
            return new ConviteResult(
                new UserInfo(existente.Id.ToString(), existente.Email, Array.Empty<string>()),
                ActionLink: null,
                JaExistia: true);
        }

        var id = Guid.NewGuid();
        var credencial = AuthCredencial.CriarParaConvite(id, emailNorm);
        await _credenciaisRepo.AdicionarAsync(credencial);

        var (cru, hashTok) = GerarTokenAleatorio();
        var token = AuthEmailToken.Emitir(
            id, AuthEmailTokenTipo.Convite, hashTok, DateTime.UtcNow.Add(TtlConvite));
        await _emailTokenRepo.AdicionarAsync(token);

        var link = MontarLink("/auth/aceitar-convite", cru);
        return new ConviteResult(
            new UserInfo(id.ToString(), emailNorm, Array.Empty<string>()),
            ActionLink: link,
            JaExistia: false);
    }

    public async Task EnviarRecuperacaoSenhaAsync(string email, string redirectTo)
    {
        // Anti-enumeração: nunca revelar se o e-mail existe — silencia falhas.
        try
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            var emailNorm = email.Trim().ToLowerInvariant();

            var credencial = await _credenciaisRepo.ObterPorEmailAsync(emailNorm);
            if (credencial is null)
            {
                _logger.LogInformation("Forgot password p/ e-mail inexistente (hash {Hash}).", HashEmail(emailNorm));
                return;
            }

            var (cru, hashTok) = GerarTokenAleatorio();
            var token = AuthEmailToken.Emitir(
                credencial.Id, AuthEmailTokenTipo.ResetSenha, hashTok, DateTime.UtcNow.Add(TtlReset));
            await _emailTokenRepo.AdicionarAsync(token);

            var link = MontarLink("/auth/redefinir-senha", cru);
            await _emails.EnviarAsync(emailNorm, "Redefinição de senha — Imedto", TemplatesEmail.ResetSenha(link));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha silenciosa em forgot-password.");
        }
    }

    // ===== Operações expostas via AuthController (não na IAuthService genérica) =====

    /// <summary>
    /// Reenvia e-mail de confirmação pra contas pendentes. Anti-enumeração:
    /// silencia se a conta não existe ou se o e-mail já está confirmado.
    /// Usado pelo endpoint POST /api/auth/reenviar-confirmacao.
    /// </summary>
    public async Task ReenviarConfirmacaoEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            var emailNorm = email.Trim().ToLowerInvariant();

            var credencial = await _credenciaisRepo.ObterPorEmailAsync(emailNorm);
            if (credencial is null || credencial.EmailConfirmado)
            {
                _logger.LogInformation("Reenvio de confirmação solicitado para {Hash} (sem efeito).", HashEmail(emailNorm));
                return;
            }

            var (cru, hashTok) = GerarTokenAleatorio();
            var token = AuthEmailToken.Emitir(
                credencial.Id, AuthEmailTokenTipo.ConfirmacaoEmail, hashTok, DateTime.UtcNow.Add(TtlConfirmacao));
            await _emailTokenRepo.AdicionarAsync(token);

            var link = MontarLink("/auth/confirmar-email", cru);
            await _emails.EnviarAsync(emailNorm, "Confirme seu e-mail no Imedto", TemplatesEmail.Confirmacao(link));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha silenciosa em reenviar-confirmacao.");
        }
    }

    /// <summary>Confirma e-mail consumindo um token. Usado pelo endpoint POST /api/auth/confirmar-email.</summary>
    public async Task ConfirmarEmailAsync(string tokenCru)
    {
        var hash = HashTokenSimples(tokenCru);
        var token = await _emailTokenRepo.ObterValidoPorHashAsync(hash, AuthEmailTokenTipo.ConfirmacaoEmail);
        if (token is null) throw new BusinessException("Token inválido ou expirado.");

        token.MarcarComoConsumido();
        _emailTokenRepo.Atualizar(token);

        var credencial = await _credenciaisRepo.ObterPorIdAsync(token.UsuarioId)
            ?? throw new BusinessException("Conta não encontrada.");
        credencial.ConfirmarEmail();
        _credenciaisRepo.Atualizar(credencial);
    }

    /// <summary>Redefine senha consumindo um token de reset. POST /api/auth/redefinir-senha.</summary>
    public async Task RedefinirSenhaAsync(string tokenCru, string novaSenha)
    {
        ValidarSenha(novaSenha);
        var hash = HashTokenSimples(tokenCru);
        var token = await _emailTokenRepo.ObterValidoPorHashAsync(hash, AuthEmailTokenTipo.ResetSenha);
        if (token is null) throw new BusinessException("Token inválido ou expirado.");

        token.MarcarComoConsumido();
        _emailTokenRepo.Atualizar(token);

        var credencial = await _credenciaisRepo.ObterPorIdAsync(token.UsuarioId)
            ?? throw new BusinessException("Conta não encontrada.");
        credencial.DefinirSenha(_hasher.Hash(novaSenha));
        if (!credencial.EmailConfirmado) credencial.ConfirmarEmail();
        if (credencial.Bloqueado) credencial.Desbloquear();
        _credenciaisRepo.Atualizar(credencial);

        // Revoga sessões existentes para forçar novo login com a nova senha.
        await _refreshRepo.RevogarTodosDoUsuarioAsync(credencial.Id);
    }

    /// <summary>Aceita convite: define senha e ativa credencial. POST /api/auth/aceitar-convite.</summary>
    /// <returns>UsuarioId associado, para o caller criar/ativar registros de domínio.</returns>
    public async Task<Guid> AceitarConviteAsync(string tokenCru, string novaSenha)
    {
        ValidarSenha(novaSenha);
        var hash = HashTokenSimples(tokenCru);
        var token = await _emailTokenRepo.ObterValidoPorHashAsync(hash, AuthEmailTokenTipo.Convite);
        if (token is null) throw new BusinessException("Convite inválido ou expirado.");

        token.MarcarComoConsumido();
        _emailTokenRepo.Atualizar(token);

        var credencial = await _credenciaisRepo.ObterPorIdAsync(token.UsuarioId)
            ?? throw new BusinessException("Convite não encontrado.");
        credencial.DefinirSenha(_hasher.Hash(novaSenha));
        credencial.ConfirmarEmail();
        _credenciaisRepo.Atualizar(credencial);

        return credencial.Id;
    }

    // ===== Helpers internos =====

    private async Task<AuthResult> EmitirSessaoAsync(AuthCredencial credencial)
    {
        var roles = Array.Empty<string>();
        var access = _issuer.EmitirAccessToken(credencial.Id, credencial.Email, roles);
        var refresh = _issuer.EmitirRefreshToken();

        var ip = _http?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var ua = _http?.HttpContext?.Request?.Headers.UserAgent.ToString();
        var refreshEntity = AuthRefreshToken.Emitir(credencial.Id, refresh.TokenHash, refresh.ExpiraEm, ip, ua);
        await _refreshRepo.AdicionarAsync(refreshEntity);

        return new AuthResult(
            access.Token,
            refresh.TokenCru,
            access.ExpiraEm,
            new UserInfo(credencial.Id.ToString(), credencial.Email, roles));
    }

    private string MontarLink(string path, string token)
    {
        var baseUrl = (_emailOptions.AppBaseUrl ?? "https://app.imedto.com").TrimEnd('/');
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }

    private static (string cru, string hash) GerarTokenAleatorio()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var cru = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var hash = HashTokenSimples(cru);
        return (cru, hash);
    }

    private static string HashTokenSimples(string cru) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cru ?? string.Empty)));

    private static void ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-mail é obrigatório.");
        if (!email.Contains('@') || email.Length > 254)
            throw new BusinessException("E-mail inválido.");
    }

    private static void ValidarSenha(string senha)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < SenhaMinima)
            throw new BusinessException($"Senha deve ter no mínimo {SenhaMinima} caracteres.");
    }

    private static string HashEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "(vazio)";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant())))[..16];
    }
}
