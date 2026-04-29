namespace Imedto.Backend.Contracts.Relatorios.Queries.Results;

/// <summary>
/// Saída do <c>RelatorioOperacionalQuery</c>. DTO único com campos opcionais para os 3
/// sub-tipos (<c>dashboard</c>, <c>agenda</c>, <c>inventario</c>) — apenas a sub-seção
/// solicitada vem preenchida; as demais ficam <see langword="null"/>. Substitui os RPCs
/// rpc_report_dashboard_summary, rpc_report_agenda_summary e rpc_report_inventory_summary.
/// </summary>
public class RelatorioOperacionalDto
{
    public string Tipo { get; set; } = string.Empty;
    public DashboardKpisDto? Dashboard { get; set; }
    public AgendaResumoDto? Agenda { get; set; }
    public InventarioResumoDto? Inventario { get; set; }
}

public class DashboardKpisDto
{
    public int TotalAgendamentos { get; set; }
    public int AgendamentosConcluidos { get; set; }
    public int AgendamentosCancelados { get; set; }
    public decimal TaxaOcupacao { get; set; }
    public decimal TaxaCancelamento { get; set; }
    public decimal Faturamento { get; set; }
    public decimal Despesas { get; set; }
    public decimal LucroLiquido { get; set; }
    public decimal TicketMedio { get; set; }
    public int NovosPacientes { get; set; }
}

public class AgendaResumoDto
{
    public int Total { get; set; }
    public IList<RowSummary> PorStatus { get; set; } = new List<RowSummary>();
    public IList<RowSummary> PorProfissional { get; set; } = new List<RowSummary>();
    public IList<RowSummary> PorDiaSemana { get; set; } = new List<RowSummary>();
}

public class InventarioResumoDto
{
    public int TotalItens { get; set; }
    public int ItensAbaixoMinimo { get; set; }
    public decimal ValorTotalEstoque { get; set; }
    public IList<RowSummary> TopMovimentacoes { get; set; } = new List<RowSummary>();
}
