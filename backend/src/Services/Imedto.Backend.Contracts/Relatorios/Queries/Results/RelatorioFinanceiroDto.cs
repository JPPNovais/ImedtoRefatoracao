namespace Imedto.Backend.Contracts.Relatorios.Queries.Results;

/// <summary>
/// Saída do <c>RelatorioFinanceiroQuery</c>. Cobre fluxo de caixa, resumo financeiro e
/// breakdown por categoria/forma de pagamento — todos os 3 RPCs legados
/// (rpc_report_cash_flow, rpc_report_financial_summary, rpc_report_financial_by_category).
/// O conteúdo de <see cref="Breakdown"/> depende de <c>agruparPor</c>.
/// </summary>
public class RelatorioFinanceiroDto
{
    public decimal TotalReceitas { get; set; }
    public decimal TotalDespesas { get; set; }
    public decimal Saldo { get; set; }
    public IList<RowSummary> Breakdown { get; set; } = new List<RowSummary>();
}
