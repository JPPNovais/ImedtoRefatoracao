using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// Emite JWT assinado com ECDSA P-256 (ES256) e refresh tokens criptograficamente
/// seguros. A chave privada vem do AWS SSM (PEM PKCS#8) via <see cref="JwtAuthOptions"/>.
/// </summary>
public class EcdsaJwtTokenIssuer : IJwtTokenIssuer, IDisposable
{
    private readonly JwtAuthOptions _options;
    private readonly ECDsa _privateKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public EcdsaJwtTokenIssuer(IOptions<JwtAuthOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPem))
            throw new InvalidOperationException("Auth:Jwt:PrivateKeyPem não configurado.");

        // pull-secrets.sh codifica PEMs como '\n' literal (.env não suporta multi-linha).
        var privPem = _options.PrivateKeyPem.Replace("\\n", "\n");

        _privateKey = ECDsa.Create();
        _privateKey.ImportFromPem(privPem.AsSpan());

        var key = new ECDsaSecurityKey(_privateKey);
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);

        // O JwtSecurityTokenHandler default mapeia claims pra URIs longas; preferimos
        // os nomes curtos JWT padrão (sub, email, etc.). MapInboundClaims = false não
        // existe no handler de saída, mas sim na configuração do JwtBearer; aqui o que
        // importa é não setar mapeamentos saindo.
    }

    public JwtTokenEmitido EmitirAccessToken(Guid usuarioId, string email, IEnumerable<string> roles)
    {
        var emiteEm = DateTime.UtcNow;
        var expiraEm = emiteEm.Add(_options.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new("sub", usuarioId.ToString()),
            new("email", email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (roles is not null)
            claims.AddRange(roles.Where(r => !string.IsNullOrWhiteSpace(r))
                                 .Select(r => new Claim("roles", r)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: emiteEm,
            expires: expiraEm,
            signingCredentials: _signingCredentials);

        var encoded = _handler.WriteToken(token);
        return new JwtTokenEmitido(encoded, expiraEm);
    }

    public RefreshTokenEmitido EmitirRefreshToken()
    {
        // 32 bytes aleatórios → string base64 url-safe (~43 chars).
        var bytes = RandomNumberGenerator.GetBytes(32);
        var cru = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var hash = HashRefreshToken(cru);
        var expiraEm = DateTime.UtcNow.Add(_options.RefreshTokenLifetime);
        return new RefreshTokenEmitido(cru, hash, expiraEm);
    }

    /// <summary>
    /// Calcula o hash SHA-256 de um refresh token cru. Exposto como helper estático
    /// para o <c>LocalJwtAuthService</c> hashear o token recebido do cookie ao
    /// procurar no banco — o token cru nunca é persistido.
    /// </summary>
    public static string HashRefreshToken(string tokenCru)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(tokenCru);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public void Dispose()
    {
        _privateKey?.Dispose();
        GC.SuppressFinalize(this);
    }
}
