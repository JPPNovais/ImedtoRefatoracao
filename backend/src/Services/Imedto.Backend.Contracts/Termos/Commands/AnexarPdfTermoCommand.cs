using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Anexa um PDF assinado fisicamente a um termo emitido pendente. O handler valida
/// magic bytes (<c>%PDF-</c>), MIME, tamanho e armazena no S3 antes de mudar o status
/// para Assinado.
/// </summary>
public class AnexarPdfTermoCommand : ICommand
{
    public long TermoEmitidoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }

    public string NomeOriginal { get; set; }
    public string MimeType { get; set; }
    public long TamanhoBytes { get; set; }
    public Stream Conteudo { get; set; }

    /// <summary>Preenchidos pelo handler.</summary>
    public string StoragePath { get; set; }
    public string PdfHash { get; set; }
}
