namespace Imedto.Backend.Domain.Prontuarios;

public interface IProntuarioAnexoRepository
{
    Task<ProntuarioAnexo> ObterPorId(long id);
    Task Salvar(ProntuarioAnexo anexo);
}

/// <summary>
/// Abstração do backend de armazenamento de blobs (hoje Supabase Storage).
/// Desacoplada para permitir plug de S3 direto ou outro provedor no futuro.
/// </summary>
public interface IAnexoStorageService
{
    Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default);
    Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300);
}
