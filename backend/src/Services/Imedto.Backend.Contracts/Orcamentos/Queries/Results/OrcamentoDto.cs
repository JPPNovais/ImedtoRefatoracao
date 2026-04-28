namespace Imedto.Backend.Contracts.Orcamentos.Queries.Results;

public class OrcamentoResumoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly Validade { get; set; }
    public decimal Total { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public class OrcamentoDto : OrcamentoResumoDto
{
    public string? Observacoes { get; set; }
    public List<ItemOrcamentoDto> Itens { get; set; } = new();
}

public class ItemOrcamentoDto
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal DescontoPercent { get; set; }
    public decimal Subtotal { get; set; }
}
