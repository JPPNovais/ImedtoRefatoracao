using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Storage;

/// <summary>
/// Adaptador para o Supabase Storage. Upload síncrono através do backend (proxy);
/// download sai como URL assinada temporária (o backend nunca baixa o blob, só emite o link).
/// </summary>
public class SupabaseStorageService : IAnexoStorageService
{
    public const string BucketAnexos = "prontuario-anexos";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        IHttpClientFactory httpClientFactory,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var url = $"/storage/v1/object/{BucketAnexos}/{path.TrimStart('/')}";

        using var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

        var content = new StreamContent(conteudo);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType);
        msg.Content = content;

        var resp = await client.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Falha ao subir anexo para {Path}: HTTP {Status} — {Body}", path, resp.StatusCode, body);
            throw new BusinessException("Não foi possível enviar o anexo.");
        }
    }

    public async Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var url = $"/storage/v1/object/sign/{BucketAnexos}/{path.TrimStart('/')}";

        var body = JsonSerializer.Serialize(new { expiresIn = ttlSegundos });
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);

        var resp = await client.SendAsync(msg);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao gerar URL assinada para {Path}: HTTP {Status} — {Body}", path, resp.StatusCode, err);
            throw new BusinessException("Não foi possível gerar o link do anexo.");
        }

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var signed = doc.RootElement.GetProperty("signedURL").GetString();

        // O Storage retorna um path relativo — precisamos prefixar com a URL do projeto.
        if (signed!.StartsWith("/"))
            return $"{_options.Url.TrimEnd('/')}/storage/v1{signed}";
        return signed;
    }
}
