using Imedto.Backend.Domain.Termos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Converte 1 ou 2 imagens (JPG/PNG) em um PDF multi-página usando QuestPDF.
/// Cada imagem ocupa uma página A4 retrato com margens de 12pt,
/// mantendo a proporção original da imagem (FitArea).
///
/// Sem fontes customizadas — o documento é apenas visual (imagem embutida).
/// A licença QuestPDF Community cobre uso sem restrição de operação.
/// </summary>
public sealed class QuestPdfImagemConverter : ITermoImagemParaPdfConverter
{
    static QuestPdfImagemConverter()
    {
        // Idempotente — se a licença já foi configurada pelo QuestPdfTermoService, sem efeito.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ConverterParaPdf(IReadOnlyList<(Stream Stream, string MimeType)> imagens)
    {
        // Buffer de todos os streams antes de gerar o PDF
        // (QuestPDF precisa dos bytes disponíveis síncronos durante o Document.GeneratePdf).
        var buffers = new List<byte[]>(imagens.Count);
        foreach (var (stream, _) in imagens)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            buffers.Add(ms.ToArray());
        }

        return Document.Create(container =>
        {
            foreach (var dados in buffers)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(12, Unit.Point);
                    page.Content().Image(dados).FitArea();
                });
            }
        }).GeneratePdf();
    }
}
