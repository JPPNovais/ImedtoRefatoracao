using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Common;

namespace Imedto.Backend.Infrastructure.Storage;

/// <summary>
/// Implementação S3 para fotos públicas (avatar de profissional, logo de
/// estabelecimento). Bucket é privado — entrega via presigned URL com TTL longo
/// (1 dia) suficiente pra cache de browser sem comprometer LGPD.
/// </summary>
public class S3FotoStorageService : IFotoStorageService
{
    private const int TtlPresignedUrlSegundos = 86_400; // 24h — cache de browser amigável

    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public S3FotoStorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> UploadFotoAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        var key = NormalizarKey(path);
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketFotos,
            Key = key,
            InputStream = conteudo,
            ContentType = mimeType,
            // SSE-S3 (AES256) já é default do bucket — não precisa redeclarar aqui.
        }, ct);

        // Retorna presigned URL com TTL de 24h (cache amigável).
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketFotos,
            Key = key,
            Expires = DateTime.UtcNow.AddSeconds(TtlPresignedUrlSegundos),
            Verb = HttpVerb.GET
        };
        return _s3.GetPreSignedURL(req);
    }

    public async Task RemoverFotoAsync(string path, CancellationToken ct = default)
    {
        var key = NormalizarKey(path);
        // S3 DeleteObject é idempotente — 204 mesmo se a chave não existir.
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketFotos,
            Key = key,
        }, ct);
    }

    private static string NormalizarKey(string path) =>
        path?.TrimStart('/') ?? throw new ArgumentNullException(nameof(path));
}
