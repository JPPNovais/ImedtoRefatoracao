namespace Imedto.Backend.Contracts.Relatorios.Queries.Results;

/// <summary>
/// Linha agregada genérica para relatórios. Usada pelos breakdowns dos relatórios
/// consolidados (financeiro/operacional/pessoas/orçamentos) — a chave é o nome do
/// agrupamento (ex.: "2026-04", "Consultas", "Concluido"), <see cref="Valor"/> é a métrica
/// numérica somada e <see cref="Count"/> é a contagem de registros.
/// </summary>
public class RowSummary
{
    public string Chave { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Count { get; set; }
}
