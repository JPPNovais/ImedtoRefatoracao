namespace Imedto.Backend.Contracts.PedidosExame.Queries.Results;

public class PedidoExameDto
{
    public long Id { get; set; }
    public long PacienteId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string? ProfissionalNome { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public IReadOnlyList<string> Exames { get; set; } = Array.Empty<string>();
    public string IndicacaoClinica { get; set; } = string.Empty;
    public string? Cid10 { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadoEm { get; set; }
}
