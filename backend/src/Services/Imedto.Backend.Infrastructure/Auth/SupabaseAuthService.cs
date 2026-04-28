using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Auth;

/// <summary>
/// Integração com Supabase Auth REST API.
/// Toda comunicação com Supabase ocorre aqui — o frontend nunca chama Supabase diretamente.
/// </summary>
public class SupabaseAuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAuthService> _logger;

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public SupabaseAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SignupResult> SignupAsync(string email, string password)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var body = JsonSerializer.Serialize(new { email, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/auth/v1/signup", content);

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Signup falhou para {Email}: HTTP {Status} — {Body}", email, response.StatusCode, erro);

            // Supabase retorna 422 com code "user_already_exists" quando e-mail está em uso.
            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity && erro.Contains("already", StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("Já existe uma conta com este e-mail.");

            throw new BusinessException("Não foi possível criar a conta.");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Dois formatos possíveis:
        //   (A) Confirmação desligada → body = { access_token, refresh_token, expires_in, user }
        //   (B) Confirmação ligada   → body = { id, email, ... } (o próprio user no topo, sem session)
        if (root.TryGetProperty("access_token", out _))
        {
            var auth = await ParseAuthResponseAsync(root);
            return new SignupResult(auth.User, auth);
        }

        var userInfo = ParseUserInfo(root);
        return new SignupResult(userInfo, null);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var body = JsonSerializer.Serialize(new { email, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/auth/v1/token?grant_type=password", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Login falhou para {Email}: HTTP {Status}", email, response.StatusCode);
            throw new BusinessException("Credenciais inválidas.");
        }

        return await ParseAuthResponse(response);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var body = JsonSerializer.Serialize(new { refresh_token = refreshToken });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/auth/v1/token?grant_type=refresh_token", content);

        if (!response.IsSuccessStatusCode)
            throw new BusinessException("Sessão expirada. Faça login novamente.");

        return await ParseAuthResponse(response);
    }

    public async Task LogoutAsync(string accessToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("supabase");
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/v1/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            await client.SendAsync(request);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: token já pode estar expirado
            _logger.LogWarning(ex, "Falha ao invalidar token no Supabase (ignorado).");
        }
    }

    public async Task<UserInfo> GetUserAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/v1/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new BusinessException("Token inválido ou expirado.");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return ParseUserInfo(doc.RootElement);
    }

    public async Task DeleteUserAsync(string userId)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/auth/v1/admin/users/{userId}");

        // Admin operations usam service_role_key como Bearer
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Falha ao excluir usuário {UserId}: HTTP {Status}", userId, response.StatusCode);
            throw new BusinessException("Não foi possível excluir a conta.");
        }
    }

    public async Task<ConviteResult> CriarConviteAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-mail é obrigatório.");

        var client = _httpClientFactory.CreateClient("supabase");
        var body = JsonSerializer.Serialize(new { type = "invite", email });
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/v1/admin/generate_link")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Convite falhou para {Email}: HTTP {Status} — {Body}", email, response.StatusCode, erro);

            // Se o usuário já existe, reutilizamos a conta e só criamos o vínculo no backend.
            var jaRegistrado = erro.Contains("already", StringComparison.OrdinalIgnoreCase)
                || erro.Contains("registered", StringComparison.OrdinalIgnoreCase)
                || erro.Contains("exists", StringComparison.OrdinalIgnoreCase);

            if (jaRegistrado)
            {
                var existente = await BuscarUsuarioPorEmailAsync(email);
                if (existente is not null)
                    return new ConviteResult(existente, null, JaExistia: true);
            }

            throw new BusinessException("Não foi possível enviar o convite. Verifique o e-mail.");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // generate_link retorna os campos do user diretamente no root (id, email, app_metadata...)
        // + action_link, email_otp, hashed_token, verification_type, redirect_to.
        var user = ParseUserInfo(root);
        var actionLink = root.TryGetProperty("action_link", out var alEl) ? alEl.GetString() : null;

        return new ConviteResult(user, actionLink, JaExistia: false);
    }

    public async Task EnviarRecuperacaoSenhaAsync(string email, string redirectTo)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("supabase");
            var body = JsonSerializer.Serialize(new { email, redirect_to = redirectTo });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await client.PostAsync("/auth/v1/recover", content);
        }
        catch (Exception ex)
        {
            // Nunca revelar se o e-mail existe ou não (prevenção de enumeração).
            _logger.LogWarning(ex, "Falha ao enviar recuperação de senha para {Email} (ignorado).", email);
        }
    }

    private async Task<UserInfo> BuscarUsuarioPorEmailAsync(string email)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var url = $"/auth/v1/admin/users?email={Uri.EscapeDataString(email)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var u in users.EnumerateArray())
        {
            var emailEl = u.TryGetProperty("email", out var eEl) ? eEl.GetString() : null;
            if (string.Equals(emailEl, email, StringComparison.OrdinalIgnoreCase))
                return ParseUserInfo(u);
        }

        return null;
    }

    // ---- Helpers ----

    private static async Task<AuthResult> ParseAuthResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return await ParseAuthResponseAsync(doc.RootElement);
    }

    private static Task<AuthResult> ParseAuthResponseAsync(JsonElement root)
    {
        var accessToken = root.GetProperty("access_token").GetString();
        var refreshToken = root.GetProperty("refresh_token").GetString();
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var user = ParseUserInfo(root.GetProperty("user"));

        return Task.FromResult(new AuthResult(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddSeconds(expiresIn),
            user));
    }

    private static UserInfo ParseUserInfo(JsonElement userElement)
    {
        var id = userElement.GetProperty("id").GetString();
        var email = userElement.GetProperty("email").GetString();

        // Supabase armazena roles em app_metadata.roles (customizável)
        var roles = Array.Empty<string>();
        if (userElement.TryGetProperty("app_metadata", out var meta) &&
            meta.TryGetProperty("roles", out var rolesEl))
        {
            roles = rolesEl.EnumerateArray()
                           .Select(r => r.GetString())
                           .Where(r => r != null)
                           .ToArray();
        }

        return new UserInfo(id, email, roles);
    }
}
