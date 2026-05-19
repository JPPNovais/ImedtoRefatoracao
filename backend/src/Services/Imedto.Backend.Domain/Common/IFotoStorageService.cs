namespace Imedto.Backend.Domain.Common;

/// <summary>
/// Storage de fotos privadas (avatar de profissional, logo de estabelecimento).
/// O bucket é privado — leitura via presigned URL com TTL curto. Persistimos
/// APENAS o caminho da chave (S3 key) no banco. A URL fresh é resolvida toda
/// vez que o front pedir, em <see cref="GerarUrlLeitura"/>. Persistir a URL
/// completa quebrava as imagens depois do TTL (24h).
/// </summary>
public interface IFotoStorageService
{
    /// <summary>
    /// Sobe a foto e devolve o caminho (S3 key) — não a URL. O caller deve
    /// persistir esse caminho no banco. Se já existir um arquivo no path,
    /// é sobrescrito.
    /// </summary>
    Task<string> UploadFotoAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default);

    /// <summary>
    /// Gera uma presigned URL fresh para o caminho dado (TTL 24h). Tolera
    /// valores legacy: se o argumento já for uma URL completa (dado antigo
    /// no banco antes da migração de schema), devolve sem assinar.
    /// Retorna null para entrada vazia.
    /// </summary>
    string? GerarUrlLeitura(string? path);

    /// <summary>
    /// Remove a foto do bucket. Idempotente: se o objeto não existe, não lança —
    /// o caller (handler que limpa o caminho) precisa que a operação seja
    /// "lossy-safe" para não travar UX quando o S3 e o banco saem de sincronia.
    /// O caller compõe o path da mesma forma que em <see cref="UploadFotoAsync"/>.
    /// </summary>
    Task RemoverFotoAsync(string path, CancellationToken ct = default);
}
