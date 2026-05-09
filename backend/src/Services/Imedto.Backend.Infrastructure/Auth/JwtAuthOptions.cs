namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// Configuração do JWT local.
///
/// Em produção/dev, todos os campos vêm do AWS SSM Parameter Store via
/// <c>/imedto/dev/jwt/*</c>; em testes, valores fixos são suficientes.
///
/// Pepper do BCrypt e api key do Resend ficam em <see cref="BcryptOptions"/> e
/// <see cref="EmailOptions"/> — separação por responsabilidade.
/// </summary>
public class JwtAuthOptions
{
    public const string Section = "Auth:Jwt";

    /// <summary>Issuer publicado no claim <c>iss</c>. Ex.: <c>imedto-backend</c>.</summary>
    public string Issuer { get; set; }

    /// <summary>Audience exigida no claim <c>aud</c>. Ex.: <c>imedto-app</c>.</summary>
    public string Audience { get; set; }

    /// <summary>Chave privada EC P-256 em PEM (PKCS#8). Usada SOMENTE para assinar.</summary>
    public string PrivateKeyPem { get; set; }

    /// <summary>Chave pública EC P-256 em PEM. Usada para validar tokens recebidos.</summary>
    public string PublicKeyPem { get; set; }

    /// <summary>Duração do access token. Default: 15 min.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Duração do refresh token. Default: 30 dias.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);
}

public class BcryptOptions
{
    public const string Section = "Auth:Bcrypt";

    /// <summary>Custo do BCrypt (10–14). 12 é o sweet spot pra hardware atual.</summary>
    public int WorkFactor { get; set; } = 12;

    /// <summary>
    /// Pepper aplicado via HMAC-SHA256(pepper, senha) antes do bcrypt. Mantido fora
    /// do banco — vazamento do dump não compromete senhas mesmo se atacante tiver hashes.
    /// </summary>
    public string Pepper { get; set; }
}

public class EmailOptions
{
    public const string Section = "Email";

    /// <summary>Endereço remetente. Ex.: <c>noreply@imedto.com</c>.</summary>
    public string From { get; set; }

    /// <summary>Domínio público da app — usado pra montar links de confirmação/reset.</summary>
    public string AppBaseUrl { get; set; }

    /// <summary>API key do Resend (Bearer).</summary>
    public string ResendApiKey { get; set; }
}
