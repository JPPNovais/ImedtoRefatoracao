namespace Imedto.Backend.Infrastructure.Storage;

/// <summary>
/// Opções de governança do Storage de anexos sensíveis (prontuário) e fotos.
/// Centraliza nomes de bucket, TTL de URL assinada e limite de tamanho — tudo configurável
/// por ambiente em <c>appsettings*.json</c> sob a seção <see cref="Section"/>.
/// </summary>
public class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Região AWS do S3. Ex.: <c>sa-east-1</c>.</summary>
    public string Region { get; set; } = "sa-east-1";

    /// <summary>Bucket privado de anexos de prontuário (download via presigned URL).</summary>
    public string BucketAnexosProntuario { get; set; } = "imedto-anexos-155684258219";

    /// <summary>Bucket privado de fotos (servidas via presigned URL — TTL maior, sem cache).</summary>
    public string BucketFotos { get; set; } = "imedto-fotos-155684258219";

    /// <summary>TTL (em minutos) das URLs assinadas de leitura. Default: 5 min.</summary>
    public int TtlSignedUrlMinutos { get; set; } = 5;

    /// <summary>Tamanho máximo (em MB) por anexo. Default: 50 MB.</summary>
    public int TamanhoMaxMb { get; set; } = 50;

    /// <summary>
    /// Whitelist de MIME types aceitos. Defense-in-depth — mesmo que o frontend valide,
    /// o backend é a fonte da verdade.
    /// </summary>
    public string[] MimeTypesPermitidos { get; set; } =
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/webp",
        "application/dicom"
    };
}
