namespace Imedto.Backend.Contracts.Admin.Auth.Results;

/// <summary>
/// Resultado de login de admin global. Retornado em 200.
///
/// O <c>accessToken</c> fica em cookie HttpOnly (não no body em produção).
/// O body contém metadados para o front decidir o fluxo (ex: forçar reset de senha).
/// </summary>
public record AdminLoginResult(
    Guid Id,
    string Email,
    string Nome,
    bool MustResetPassword);

/// <summary>
/// Dados do admin autenticado, retornados por GET /api/admin/auth/me.
/// </summary>
public record AdminMeResult(
    Guid Id,
    string Email,
    string Nome,
    bool Ativo,
    bool ForcePasswordReset,
    DateTimeOffset? UltimoLoginEm);

/// <summary>
/// Payload de change-password.
///
/// <see cref="SenhaAtual"/> é opcional no contrato:
/// - Troca voluntária (token regular, sem <c>must_reset_password</c>): obrigatória — validada no handler.
/// - Força-reset (token com <c>must_reset_password = true</c>): ignorada mesmo se enviada.
/// A distinção é feita no handler pela claim, não pela presença do campo.
/// </summary>
public record AdminChangePasswordRequest(string NovaSenha, string? SenhaAtual = null);
