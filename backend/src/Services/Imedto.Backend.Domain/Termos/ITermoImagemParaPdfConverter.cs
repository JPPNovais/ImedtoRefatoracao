namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Converte 1 ou 2 imagens (JPG ou PNG) em um PDF multi-página.
/// Cada imagem ocupa uma página inteira em A4 retrato com margens mínimas.
/// O PDF resultante é o documento que vai para o S3 e tem o SHA-256 calculado.
///
/// Implementação concreta: <c>QuestPdfImagemConverter</c> (Infrastructure).
/// </summary>
public interface ITermoImagemParaPdfConverter
{
    /// <summary>
    /// Converte as imagens em PDF. Os streams devem estar posicionados em zero.
    /// </summary>
    /// <param name="imagens">Lista de (stream, mimeType) — máximo 2 itens.</param>
    /// <returns>Bytes do PDF resultante.</returns>
    byte[] ConverterParaPdf(IReadOnlyList<(Stream Stream, string MimeType)> imagens);
}
