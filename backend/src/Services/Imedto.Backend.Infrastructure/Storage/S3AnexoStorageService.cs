using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Storage;

/// <summary>
/// Implementação S3 para anexos sensíveis de prontuário. Bucket privado, acesso
/// SOMENTE via presigned URL gerada pelo backend (TTL curto — default 5 min).
/// </summary>
public class S3AnexoStorageService : IAnexoStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public S3AnexoStorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        var key = NormalizarKey(path);
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketAnexosProntuario,
            Key = key,
            InputStream = conteudo,
            ContentType = mimeType,
        }, ct);
    }

    public Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300)
    {
        var key = NormalizarKey(path);
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketAnexosProntuario,
            Key = key,
            Expires = DateTime.UtcNow.AddSeconds(Math.Max(60, ttlSegundos)),
            Verb = HttpVerb.GET
        };
        return Task.FromResult(_s3.GetPreSignedURL(req));
    }

    private static string NormalizarKey(string path) =>
        path?.TrimStart('/') ?? throw new ArgumentNullException(nameof(path));
}
