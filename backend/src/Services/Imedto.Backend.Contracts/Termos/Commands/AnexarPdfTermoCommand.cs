using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Anexa o documento assinado fisicamente a um termo emitido pendente.
/// Aceita 1-2 imagens JPG/PNG (convertidas em PDF multi-página no backend) ou 1 PDF.
/// O handler valida magic bytes, MIME, tamanho e armazena no S3.
/// </summary>
public class AnexarPdfTermoCommand : ICommand
{
    public long TermoEmitidoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }

    /// <summary>
    /// Lista de partes do upload: cada item é (stream, mimeType, tamanho).
    /// Para PDF: 1 item. Para foto: 1-2 itens (frente e verso).
    /// </summary>
    public IReadOnlyList<(Stream Stream, string MimeType, long TamanhoBytes)> Partes { get; set; }

    /// <summary>Soma dos tamanhos de todas as partes (verificado antes de processar).</summary>
    public long TamanhoTotalBytes { get; set; }

    /// <summary>Preenchidos pelo handler.</summary>
    public string StoragePath { get; set; }
    public string PdfHash { get; set; }
}
