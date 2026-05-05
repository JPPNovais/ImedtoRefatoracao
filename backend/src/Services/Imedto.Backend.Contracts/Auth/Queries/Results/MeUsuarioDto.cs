namespace Imedto.Backend.Contracts.Auth.Queries.Results;

/// <summary>
/// Forma serializada do usuário retornada por /auth/me, /auth/refresh e /auth/bootstrap.
/// Record imutável para permitir caching seguro em memória (ver AuthController).
/// Payload minimizado por LGPD — cpf e ultimoAcessoEm não são expostos.
/// </summary>
public record MeUsuarioDto(
    Guid Id,
    string Email,
    string NomeCompleto,
    string Telefone,
    string Status,
    bool OnboardingCompleto);
