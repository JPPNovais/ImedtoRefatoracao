using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Infrastructure.Storage;

/// <summary>
/// Adaptador para o bucket público <c>imedto-fotos</c> no Supabase Storage.
/// Diferente de <see cref="SupabaseStorageService"/> (anexos privados), aqui a URL final
/// é direta/pública e não precisa renovação — pode ser usada como <c>&lt;img src&gt;</c>.
/// </summary>
public class SupabaseFotoStorageService : IFotoStorageService
{
    public const string Bucket = "imedto-fotos";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseFotoStorageService> _logger;

    public SupabaseFotoStorageService(
        IHttpClientFactory httpClientFactory,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseFotoStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadFotoAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("supabase");
        var pathLimpo = path.TrimStart('/');
        var url = $"/storage/v1/object/{Bucket}/{pathLimpo}";

        using var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        // Sobrescreve a foto anterior (mesmo path) — evita acúmulo de arquivos órfãos.
        msg.Headers.Add("x-upsert", "true");

        var content = new StreamContent(conteudo);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType);
        msg.Content = content;

        var resp = await client.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Falha ao subir foto para {Path}: HTTP {Status} — {Body}", path, resp.StatusCode, body);
            throw new BusinessException("Não foi possível enviar a foto.");
        }

        // URL pública direta + cache-buster para forçar reload imediato no browser.
        var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"{_options.Url.TrimEnd('/')}/storage/v1/object/public/{Bucket}/{pathLimpo}?v={cacheBuster}";
    }
}
