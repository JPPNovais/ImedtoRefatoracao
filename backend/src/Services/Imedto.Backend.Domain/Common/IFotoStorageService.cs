namespace Imedto.Backend.Domain.Common;

/// <summary>
/// Storage de fotos públicas (avatar de profissional, logo de estabelecimento).
/// As fotos vão para um bucket público no S3 e a URL final é direta
/// (sem signed URL) para servir como <c>&lt;img src&gt;</c> sem renovação.
/// </summary>
public interface IFotoStorageService
{
    /// <summary>
    /// Sobe a foto para o bucket público de fotos e devolve a URL final.
    /// O caller compõe o path (ex.: "profissionais/{usuarioId}.jpg").
    /// Se já existir um arquivo no path, é sobrescrito.
    /// </summary>
    Task<string> UploadFotoAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default);

    /// <summary>
    /// Remove a foto do bucket. Idempotente: se o objeto não existe, não lança —
    /// o caller (handler que limpa <c>FotoUrl</c>) precisa que a operação seja
    /// "lossy-safe" para não travar UX quando o S3 e o banco saem de sincronia.
    /// O caller compõe o path da mesma forma que em <see cref="UploadFotoAsync"/>.
    /// </summary>
    Task RemoverFotoAsync(string path, CancellationToken ct = default);
}
