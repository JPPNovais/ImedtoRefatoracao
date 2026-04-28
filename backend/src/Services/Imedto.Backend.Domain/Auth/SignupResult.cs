namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Resultado de um signup no Supabase Auth.
/// </summary>
/// <param name="User">Dados do usuário recém-criado (sempre presente).</param>
/// <param name="Session">Sessão ativa, ou <c>null</c> se o projeto exige confirmação de e-mail.</param>
public record SignupResult(UserInfo User, AuthResult Session)
{
    public bool RequerConfirmacaoEmail => Session is null;
}
