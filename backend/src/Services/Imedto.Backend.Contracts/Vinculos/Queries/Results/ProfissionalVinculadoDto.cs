namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

public class ProfissionalVinculadoDto
{
    public long VinculoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Email { get; set; }
    public string NomeCompleto { get; set; }
    public string Status { get; set; }
    public long? ModeloPermissaoId { get; set; }
    public string ModeloPermissaoNome { get; set; }
    public DateTime ConvidadoEm { get; set; }
    public DateTime? AceitoEm { get; set; }
    public string? Especialidade { get; set; }
    public string? Conselho { get; set; }
}
