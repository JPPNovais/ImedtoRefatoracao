using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Relatorios.Queries;

/// <summary>
/// Relatório operacional. <see cref="Tipo"/> aceita <c>dashboard</c> (KPIs gerais),
/// <c>agenda</c> (resumo da agenda) ou <c>inventario</c> (estoque). Substitui
/// rpc_report_dashboard_summary, rpc_report_agenda_summary e rpc_report_inventory_summary.
/// </summary>
public class RelatorioOperacionalQuery : IQuery<RelatorioOperacionalDto>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public string Tipo { get; set; } = "dashboard";
}
