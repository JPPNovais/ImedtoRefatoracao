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
///
/// Governança LGPD aplicada aqui (defense-in-depth — não confia em validação do frontend):
///  - Whitelist de MIME (<see cref="StorageOptions.MimeTypesPermitidos"/>).
///  - Limite de tamanho (<see cref="StorageOptions.TamanhoMaxMb"/>).
///  - Sanitização de path para impedir path traversal (<c>..</c>, <c>/</c>, <c>\</c>).
/// </summary>
public class SupabaseStorageService : IAnexoStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _supabaseOptions;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        IHttpClientFactory httpClientFactory,
        IOptions<SupabaseOptions> supabaseOptions,
        IOptions<StorageOptions> storageOptions,
        ILogger<SupabaseStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _supabaseOptions = supabaseOptions.Value;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        // Validação 1: MIME whitelist (defense-in-depth — frontend já filtra, mas back é fonte da verdade).
        if (string.IsNullOrWhiteSpace(mimeType) ||
            !_storageOptions.MimeTypesPermitidos.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException("Tipo de arquivo não permitido.");
        }

        // Validação 2: tamanho. Tenta usar Length quando disponível; caso contrário,
        // o limite real é aplicado no handler antes de chamar o storage (TamanhoBytes do command).
        if (conteudo.CanSeek)
        {
            var limiteBytes = (long)_storageOptions.TamanhoMaxMb * 1024L * 1024L;
            if (conteudo.Length > limiteBytes)
                throw new BusinessException($"Arquivo excede o limite de {_storageOptions.TamanhoMaxMb} MB.");
        }

        // Validação 3: path traversal. Path traversal aqui = arquivo escrito fora da pasta esperada
        // do bucket. O handler já compõe o path com prefixo {est}/{prontuario}/{guid}_{nome}; reforçamos.
        GarantirPathSeguro(path);

        var bucket = _storageOptions.BucketAnexosProntuario;
        var client = _httpClientFactory.CreateClient("supabase");
        var url = $"/storage/v1/object/{bucket}/{path.TrimStart('/')}";

        using var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseOptions.ServiceRoleKey);

        var content = new StreamContent(conteudo);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
        msg.Content = content;

        var resp = await client.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            // Loga path (não-PII) e status — nunca o conteúdo do arquivo nem nome do paciente.
            _logger.LogError("Falha ao subir anexo para {Path}: HTTP {Status} — {Body}", path, resp.StatusCode, body);
            throw new BusinessException("Não foi possível enviar o anexo.");
        }
    }

    public async Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300)
    {
        GarantirPathSeguro(path);

        var bucket = _storageOptions.BucketAnexosProntuario;
        var client = _httpClientFactory.CreateClient("supabase");
        var url = $"/storage/v1/object/sign/{bucket}/{path.TrimStart('/')}";

        var body = JsonSerializer.Serialize(new { expiresIn = ttlSegundos });
        using var msg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseOptions.ServiceRoleKey);

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
            return $"{_supabaseOptions.Url.TrimEnd('/')}/storage/v1{signed}";
        return signed;
    }

    /// <summary>
    /// Bloqueia tentativas óbvias de path traversal e caracteres absolutos.
    /// O handler já compõe um path estruturado; aqui é o último anel da cebola.
    /// </summary>
    private static void GarantirPathSeguro(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new BusinessException("Caminho do arquivo inválido.");
        if (path.Contains("..", StringComparison.Ordinal))
            throw new BusinessException("Caminho do arquivo inválido.");
        if (path.StartsWith('/') || path.StartsWith('\\'))
            throw new BusinessException("Caminho do arquivo inválido.");
        if (path.Contains('\\'))
            throw new BusinessException("Caminho do arquivo inválido.");
    }
}
