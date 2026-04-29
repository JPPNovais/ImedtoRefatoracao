namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Configuração extra de pagamento do orçamento (item 7 — schema fechado para o que era
/// <c>config_pagamento_json</c> opaco). Armazenado como jsonb e (de)serializado via
/// <c>HasConversion</c> no <c>OrcamentoConfiguration</c>. Todos os campos são opcionais
/// para preservar compatibilidade com orçamentos sem configuração.
///
/// <c>DescontoPercentual</c> e <c>DescontoValor</c> são alternativos — o orçamento aplica
/// só um deles (a regra é responsabilidade do front/handler que monta o objeto).
/// <c>JurosPercentual</c> + <c>TaxaParcela</c> capturam o juros fixo + a taxa por parcela
/// usados no cálculo das formas de pagamento.
/// </summary>
public class ConfigPagamentoOrcamento
{
    public decimal? DescontoPercentual { get; init; }
    public decimal? DescontoValor { get; init; }
    public decimal? JurosPercentual { get; init; }
    public int? ParcelasMaximas { get; init; }
    public decimal? TaxaParcela { get; init; }
    public string? Observacoes { get; init; }
}
