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

    /// <summary>Service role key — permissões de admin (delete user, etc.). NUNCA expor.</summary>
    public string ServiceRoleKey { get; set; }
}
