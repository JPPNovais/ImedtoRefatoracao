using Amazon.S3;
using Amazon.S3.Model;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Storage S3 dos PDFs assinados de termos. Reaproveita o bucket privado de anexos
/// (<see cref="StorageOptions.BucketAnexosProntuario"/>) — mesma política de retenção
/// e privacidade (LGPD).
/// </summary>
public sealed class S3TermoPdfStorageService : ITermoPdfStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public S3TermoPdfStorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketAnexosProntuario,
            Key = NormalizarKey(path),
            InputStream = conteudo,
            ContentType = mimeType,
        }, ct);
    }

    public Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketAnexosProntuario,
            Key = NormalizarKey(path),
            Expires = DateTime.UtcNow.AddSeconds(Math.Max(60, ttlSegundos)),
            Verb = HttpVerb.GET,
        };
        return Task.FromResult(_s3.GetPreSignedURL(req));
    }

    private static string NormalizarKey(string path) =>
        path?.TrimStart('/') ?? throw new ArgumentNullException(nameof(path));
}
