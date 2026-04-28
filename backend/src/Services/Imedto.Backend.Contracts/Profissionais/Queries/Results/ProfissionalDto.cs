namespace Imedto.Backend.Contracts.Profissionais.Queries.Results;

public class ProfissionalDto
{
    public Guid UsuarioId { get; set; }
    public string Conselho { get; set; }
    public string Uf { get; set; }
    public string NumeroRegistro { get; set; }
    public string Especialidade { get; set; }
    public string Bio { get; set; }
    public string FotoUrl { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}
