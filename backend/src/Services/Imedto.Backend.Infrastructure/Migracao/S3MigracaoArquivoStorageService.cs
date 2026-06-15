using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Infrastructure.Storage;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Adapter S3 para upload/remoção do arquivo ZIP bruto de migração.
/// Bucket privado, SSE-S3 em repouso (default do bucket).
/// Retenção de 30 dias implementada por lifecycle rule do bucket (CA24, R12).
/// </summary>
public sealed class S3MigracaoArquivoStorageService : IMigracaoArquivoStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public S3MigracaoArquivoStorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> UploadArquivoAsync(
        long estabelecimentoId,
        long jobId,
        Stream conteudo,
        CancellationToken ct = default)
    {
        // Key: migracao/{estabelecimentoId}/{jobId}/arquivo.zip
        var key = $"migracao/{estabelecimentoId}/{jobId}/arquivo.zip";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketAnexosProntuario, // bucket privado — reutiliza mesmo bucket de anexos
            Key = key,
            InputStream = conteudo,
            ContentType = "application/zip",
        }, ct);

        return key;
    }

    public async Task RemoverArquivoAsync(string arquivoS3Key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(arquivoS3Key)) return;

        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketAnexosProntuario,
            Key = arquivoS3Key,
        }, ct);
    }
}
