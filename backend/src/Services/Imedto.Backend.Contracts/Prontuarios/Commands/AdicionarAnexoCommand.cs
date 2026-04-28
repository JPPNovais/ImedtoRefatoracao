using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Adiciona anexo a um prontuário. O controller já leu o stream do upload
/// e guardou em <see cref="Conteudo"/> — o handler faz upload no Storage via service_role
/// e registra a metadata.
/// </summary>
public class AdicionarAnexoCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? EvolucaoId { get; set; }
    public Guid AutorUsuarioId { get; set; }
    public string NomeOriginal { get; set; }
    public string MimeType { get; set; }
    public long TamanhoBytes { get; set; }
    public Stream Conteudo { get; set; }
    public long AnexoIdCriado { get; set; } // preenchido pelo handler — devolvido ao controller
    public string StoragePath { get; set; }
}
