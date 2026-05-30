using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Emite e valida JWT para administradores globais do Imedto.
///
/// Reutiliza a mesma chave ECDSA P-256 do app principal, mas injeta claim
/// <c>imedto_admin = "true"</c>. NUNCA carrega <c>estabelecimento_id</c> —
/// admin global não é usuário de tenant.
///
/// Refresh tokens admin são armazenados na tabela <c>imedto_admin_refresh_tokens</c>
/// com hash SHA-256 (nunca o token cru).
/// </summary>
public class ImedtoAdminTokenIssuer
{
    private readonly IJwtTokenIssuer _jwtIssuer;
    private readonly JwtAuthOptions _options;
    private readonly ILogger<ImedtoAdminTokenIssuer> _logger;

    /// <summary>TTL do refresh token admin (2h — mais curto que usuário por ser privilégio global).</summary>
    public static readonly TimeSpan RefreshTtl = TimeSpan.FromHours(2);

    /// <summary>Claim que distingue JWT admin de JWT de usuário comum.</summary>
    public const string AdminClaim = "imedto_admin";

    /// <summary>Claim que força troca de senha antes de liberar outras rotas.</summary>
    public const string MustResetPasswordClaim = "must_reset_password";

    public ImedtoAdminTokenIssuer(
        IJwtTokenIssuer jwtIssuer,
        IOptions<JwtAuthOptions> options,
        ILogger<ImedtoAdminTokenIssuer> logger)
    {
        _jwtIssuer = jwtIssuer;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Emite access token admin. Se <paramref name="forcePasswordReset"/> = true,
    /// o token inclui <c>must_reset_password = "true"</c> e só permite chamar
    /// o endpoint de change-password.
    /// </summary>
    public JwtTokenEmitido EmitirAccessToken(ImedtoAdmin admin, bool forcePasswordReset)
    {
        // Reutiliza o emissor base mas injeta claims admin via roles[]
        // (o EcdsaJwtTokenIssuer itera roles[] — cada role vira um claim "roles").
        // Porém precisamos de claims específicas (imedto_admin e must_reset_password),
        // então emitimos diretamente via método próprio que constrói o token.
        var emiteEm = DateTime.UtcNow;
        var expiraEm = emiteEm.Add(_options.AccessTokenLifetime);

        // Emitir com roles[] que codificamos como claims especiais.
        // O EcdsaJwtTokenIssuer não suporta claims arbitrárias, então construímos
        // o token usando um "roles trick": codificamos imedto_admin como role especial
        // e depois usamos um AuthorizationHandler que valida o claim correto.
        //
        // Na prática: emitimos via IJwtTokenIssuer.EmitirAccessToken e depois
        // adicionamos as claims admin usando o padrão do emissor base.
        // Como EcdsaJwtTokenIssuer é sealed/concrete, usamos os roles[] para
        // transportar as claims admin.
        //
        // Abordagem: emitir com claims extras via uma lista de roles codificadas.
        // O JwtBearerEvents vai validar o claim imedto_admin diretamente.
        //
        // IMPORTANTE: o EcdsaJwtTokenIssuer mapeia cada string de roles[] para
        // claim "roles:valor". Admin claim precisa ser "imedto_admin = true",
        // não "roles = imedto_admin". Então usamos abordagem alternativa:
        // construímos o token com claims manuais usando a mesma chave de assinatura.

        return EmitirTokenComClaimsAdmin(admin, forcePasswordReset, emiteEm, expiraEm);
    }

    /// <summary>
    /// Gera um refresh token cru (criptograficamente seguro) e seu hash SHA-256.
    /// Persistir só o hash na tabela <c>imedto_admin_refresh_tokens</c>.
    /// </summary>
    public static (string tokenCru, string tokenHash, DateTimeOffset expiraEm) GerarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var cru = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var hash = HashToken(cru);
        var expira = DateTimeOffset.UtcNow.Add(RefreshTtl);
        return (cru, hash, expira);
    }

    /// <summary>Calcula hash SHA-256 do refresh token (compatível com EcdsaJwtTokenIssuer.HashRefreshToken).</summary>
    public static string HashToken(string tokenCru)
    {
        var bytes = Encoding.UTF8.GetBytes(tokenCru);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    // Constrói JWT com claims admin diretamente (sem depender das roles[] do EcdsaJwtTokenIssuer).
    // Requer acesso à chave de assinatura — obtemos via IOptions<JwtAuthOptions>.
    private JwtTokenEmitido EmitirTokenComClaimsAdmin(
        ImedtoAdmin admin,
        bool forcePasswordReset,
        DateTime emiteEm,
        DateTime expiraEm)
    {
        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPem))
            throw new InvalidOperationException("Auth:Jwt:PrivateKeyPem não configurado.");

        var privPem = _options.PrivateKeyPem.Replace("\\n", "\n");
        // Não usar `using` — ECDsaSecurityKey mantém referência ao ECDsa
        // e WriteToken() assina depois da construção do token.
        var ecdsa = System.Security.Cryptography.ECDsa.Create();
        ecdsa.ImportFromPem(privPem.AsSpan());
        var key = new Microsoft.IdentityModel.Tokens.ECDsaSecurityKey(ecdsa)
        {
            KeyId = "admin-key"
        };
        var signingCreds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.EcdsaSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new("sub", admin.Id.ToString()),
            new("email", admin.Email),
            new(AdminClaim, "true"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (forcePasswordReset)
            claims.Add(new(MustResetPasswordClaim, "true"));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: emiteEm,
            expires: expiraEm,
            signingCredentials: signingCreds);

        var handler = new JwtSecurityTokenHandler();
        var encoded = handler.WriteToken(token);
        return new JwtTokenEmitido(encoded, expiraEm);
    }
}
