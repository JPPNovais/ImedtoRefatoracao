namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Porta de storage para o arquivo ZIP bruto da migração.
/// Implementação concreta usa S3 (bucket privado, retenção 30 dias — CA24, R12).
/// Arquivo criptografado em repouso por SSE-S3 do bucket.
/// </summary>
public interface IMigracaoArquivoStorageService
{
    /// <summary>
    /// Faz upload do arquivo ZIP no S3.
    /// Retorna a key gerada para persistir em <see cref="MigracaoJob.ArquivoS3Key"/>.
    /// </summary>
    Task<string> UploadArquivoAsync(
        long estabelecimentoId,
        long jobId,
        Stream conteudo,
        CancellationToken ct = default);

    /// <summary>Remove o arquivo do S3 (job de expiração — CA24).</summary>
    Task RemoverArquivoAsync(string arquivoS3Key, CancellationToken ct = default);

    /// <summary>
    /// Baixa o arquivo ZIP do S3 como stream em memória.
    /// Usado pelo job de inferência (Marco 2) para descompactar e parsear.
    /// </summary>
    Task<Stream> DownloadArquivoAsync(string arquivoS3Key, CancellationToken ct = default);
}
