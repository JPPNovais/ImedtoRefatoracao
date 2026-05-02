namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// Opções do Supabase — todas ficam SOMENTE no backend (appsettings / variáveis de ambiente).
/// Nunca exponha ServiceRoleKey ou JwtSecret ao frontend.
/// </summary>
public class SupabaseOptions
{
    public const string Section = "Supabase";

    /// <summary>URL do projeto Supabase. Ex: https://xxx.supabase.co</summary>
    public string Url { get; set; }

    /// <summary>Issuer/Authority para validação de JWT via JWKS. Ex: https://xxx.supabase.co/auth/v1</summary>
    public string Authority { get; set; }

    /// <summary>
    /// Anon key — chave publica usada como header 'apikey' default em todas as
    /// chamadas REST do Supabase (endpoints publicos: signup, login, refresh,
    /// recover; e endpoints com Bearer do usuario: logout, /user). Pode ser
    /// commitada em frontends, mas aqui fica no backend para garantir que toda
    /// chamada para o Supabase passe por nosso BFF.
    /// </summary>
    public string AnonKey { get; set; }

    /// <summary>
    /// Service role key — permissoes de admin (delete user, generate_link,
    /// admin/users). NUNCA expor para o front e NUNCA usar como apikey default
    /// (bypassa rate limit/policies do proprio Supabase). So usar em headers
    /// explicitos de operacoes admin.
    /// </summary>
    public string ServiceRoleKey { get; set; }
}
