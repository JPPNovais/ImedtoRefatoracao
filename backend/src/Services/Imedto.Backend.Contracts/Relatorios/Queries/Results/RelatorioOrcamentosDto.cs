namespace Imedto.Backend.Contracts.Relatorios.Queries.Results;

/// <summary>
/// Saída do <c>RelatorioOrcamentosQuery</c>. Substitui rpc_report_budgets_summary com
/// breakdown por status. Valor médio considera apenas orçamentos cujo total &gt; 0
/// (orçamentos sem itens persistidos não distorcem a média).
/// </summary>
public class RelatorioOrcamentosDto
{
    public int TotalEmitidos { get; set; }
    public int TotalAprovados { get; set; }
    public int TotalRecusados { get; set; }
    public decimal ValorMedio { get; set; }
    public decimal TaxaConversao { get; set; }
    public IList<RowSummary> Breakdown { get; set; } = new List<RowSummary>();
}
