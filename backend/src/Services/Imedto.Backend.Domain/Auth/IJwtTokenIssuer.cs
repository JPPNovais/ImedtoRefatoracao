namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Emite JWT assinado localmente. Implementação atual usa ECDSA P-256 (ES256)
/// com chave privada vinda do AWS SSM Parameter Store.
/// </summary>
public interface IJwtTokenIssuer
{
    /// <summary>
    /// Emite um access token. Os claims típicos vão em:
    /// - <c>sub</c>: <paramref name="usuarioId"/>
    /// - <c>email</c>: <paramref name="email"/>
    /// - <c>roles</c>: <paramref name="roles"/>
    /// </summary>
    JwtTokenEmitido EmitirAccessToken(Guid usuarioId, string email, IEnumerable<string> roles);

    /// <summary>
    /// Gera um refresh token cru (criptograficamente seguro) e seu hash SHA-256.
    /// Persistir só o hash; entregar o cru ao cliente em cookie HttpOnly.
    /// </summary>
    RefreshTokenEmitido EmitirRefreshToken();
}

public record JwtTokenEmitido(string Token, DateTime ExpiraEm);

public record RefreshTokenEmitido(string TokenCru, string TokenHash, DateTime ExpiraEm);
