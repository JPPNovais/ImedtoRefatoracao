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

public class PaginaPedidosExameDto
{
    public IEnumerable<PedidoExameDto> Itens { get; set; } = Array.Empty<PedidoExameDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
