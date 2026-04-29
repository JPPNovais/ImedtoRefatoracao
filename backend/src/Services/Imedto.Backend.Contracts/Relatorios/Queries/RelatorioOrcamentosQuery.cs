using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Relatorios.Queries;

/// <summary>
/// Relatório de orçamentos. Substitui rpc_report_budgets_summary.
/// </summary>
public class RelatorioOrcamentosQuery : IQuery<RelatorioOrcamentosDto>
{
    public long EstabelecimentoId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
}
