namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Storage de PDFs de termos assinados. Reutiliza o bucket de anexos (privado, AES256,
/// presigned URL TTL curto). A interface fica separada de <c>IAnexoStorageService</c>
/// porque o ciclo de vida do dado (não vinculado a prontuário/evolução) e a política
/// de retenção podem divergir no futuro.
/// </summary>
public interface ITermoPdfStorageService
{
    Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default);
    Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300);
}
