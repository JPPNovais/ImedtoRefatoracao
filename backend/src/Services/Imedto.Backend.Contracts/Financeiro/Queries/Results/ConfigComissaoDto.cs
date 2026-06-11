namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

/// <summary>DTO da configuração de comissão de um profissional no estabelecimento (R16).</summary>
public class ConfigComissaoDto
{
    public decimal? PercentualConsulta { get; set; }
    public decimal? PercentualProcedimento { get; set; }
    /// <summary>Default de sistema quando não configurado (R15 — 30%).</summary>
    public decimal PercentualPadrao { get; set; }
}
