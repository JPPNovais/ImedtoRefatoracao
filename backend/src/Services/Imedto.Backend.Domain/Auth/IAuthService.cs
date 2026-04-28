namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Contrato de autenticação. Implementado na Infrastructure via Supabase REST API.
/// Injetado diretamente nos controllers — não passa pelo bus CQRS (auth não é comando de domínio).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Cria uma nova conta no Supabase Auth. Retorna o <see cref="SignupResult"/>:
    /// - Se "Email confirmations" estiver desligado, vem uma sessão ativa pronta pra setar cookies.
    /// - Se ligado, a sessão vem nula e o usuário precisa confirmar o e-mail antes de fazer login.
    /// </summary>
    Task<SignupResult> SignupAsync(string email, string password);

    /// <summary>
    /// Gera um magic link de convite para um e-mail. Se o usuário ainda não tem conta,
    /// ela é criada automaticamente no Supabase Auth. O <see cref="ConviteResult.ActionLink"/>
    /// leva o convidado para definir senha e entrar logado.
    /// Usa service_role_key — requer chamada a partir do backend (BFF).
    /// </summary>
    Task<ConviteResult> CriarConviteAsync(string email);

    /// <summary>Autentica o usuário. Lança BusinessException em credenciais inválidas.</summary>
    Task<AuthResult> LoginAsync(string email, string password);

    /// <summary>Renova o access token usando o refresh token. Lança BusinessException se expirado.</summary>
    Task<AuthResult> RefreshAsync(string refreshToken);

    /// <summary>Invalida o token no Supabase. Fire-and-forget — não lança exceção.</summary>
    Task LogoutAsync(string accessToken);

    /// <summary>Retorna informações do usuário autenticado pelo token.</summary>
    Task<UserInfo> GetUserAsync(string accessToken);

    /// <summary>Exclui o usuário no Supabase (LGPD — direito ao esquecimento). Usa service_role_key.</summary>
    Task DeleteUserAsync(string userId);

    /// <summary>Envia e-mail de recuperação de senha via Supabase. Não lança exceção se o e-mail não existir (prevenção de enumeração).</summary>
    Task EnviarRecuperacaoSenhaAsync(string email, string redirectTo);
}
