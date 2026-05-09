namespace Imedto.Backend.Domain.Prontuarios;

public interface IProntuarioAnexoRepository
{
    /// <summary>
    /// Carrega o anexo filtrando pelo tenant. Retorna <c>null</c> quando o anexo
    /// não existe ou pertence a outro estabelecimento — defense-in-depth multi-tenant
    /// para impedir IDOR em download/exclusão (LGPD: anexos podem indicar diagnóstico).
    /// </summary>
    Task<ProntuarioAnexo?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(ProntuarioAnexo anexo);
}

/// <summary>
/// Abstração do backend de armazenamento de blobs (hoje AWS S3).
/// Desacoplada para permitir plug de outro provedor no futuro.
/// </summary>
public interface IAnexoStorageService
{
    Task UploadAsync(string path, Stream conteudo, string mimeType, CancellationToken ct = default);
    Task<string> GerarUrlAssinadaLeituraAsync(string path, int ttlSegundos = 300);
}
