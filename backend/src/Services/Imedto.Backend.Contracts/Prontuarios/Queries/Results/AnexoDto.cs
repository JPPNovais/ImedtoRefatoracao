namespace Imedto.Backend.Contracts.Prontuarios.Queries.Results;

public class AnexoDto
{
    public long Id { get; set; }
    public long ProntuarioId { get; set; }
    public long? EvolucaoId { get; set; }
    public string NomeOriginal { get; set; }
    public string MimeType { get; set; }
    public long TamanhoBytes { get; set; }
    public DateTime CriadoEm { get; set; }
    public string AutorNome { get; set; }
}

public class AnexoUrlDto
{
    public long Id { get; set; }
    public string NomeOriginal { get; set; }
    public string MimeType { get; set; }
    public string Url { get; set; }
    public DateTime ExpiraEm { get; set; }
}
