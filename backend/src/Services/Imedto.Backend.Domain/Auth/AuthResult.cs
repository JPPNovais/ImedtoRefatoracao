namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Resultado de uma operação de autenticação.
/// Nunca exposto ao frontend — o backend usa para montar os cookies.
/// </summary>
public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User
);

public record UserInfo(
    string Id,
    string Email,
    string[] Roles
);
