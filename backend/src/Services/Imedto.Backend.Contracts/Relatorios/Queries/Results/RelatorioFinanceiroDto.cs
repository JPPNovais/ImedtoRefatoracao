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

    /// <summary>
    /// F7/R20 — Visão por paciente (custo/lucro). Null quando não solicitada.
    /// Adicionado de forma aditiva — clientes existentes ignoram o campo (R25).
    /// </summary>
    public IList<CustoLucroPacienteDto>? PorPaciente { get; set; }
}

/// <summary>
/// Custo/lucro por paciente (F7/R20 — R22 audita o drill-down, não a listagem).
/// LGPD: nome do paciente no DTO minimizado (só id + nome).
/// </summary>
public class CustoLucroPacienteDto
{
    public long PacienteId { get; set; }
    public string PacienteNome { get; set; } = string.Empty;
    public decimal Cobrado { get; set; }
    public decimal Pago { get; set; }
    public decimal Desconto { get; set; }
    public decimal Taxa { get; set; }
    public decimal Custo { get; set; }
    public decimal Lucro { get; set; }  // = Pago − Custo
}
