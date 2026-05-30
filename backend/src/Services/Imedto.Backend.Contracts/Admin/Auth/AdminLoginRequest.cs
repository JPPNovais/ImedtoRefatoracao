namespace Imedto.Backend.Contracts.Admin.Auth;

/// <param name="Email">E-mail do administrador.</param>
/// <param name="Senha">Senha do administrador.</param>
public record AdminLoginRequest(string Email, string Senha);
