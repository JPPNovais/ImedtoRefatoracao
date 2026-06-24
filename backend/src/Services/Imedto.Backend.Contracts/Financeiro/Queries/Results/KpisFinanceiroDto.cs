namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

/// <summary>
/// KPIs do período para a aba "Visão geral" do /financeiro (R1/CA156).
/// Calculados 100% no backend via SUM/GROUP BY — nunca somado no front.
/// </summary>
public class KpisFinanceiroDto
{
    // KPIs primários
    public decimal Recebido { get; set; }       // receitas pagas (inclui estornos abatendo — R1)
    public decimal AReceber { get; set; }       // saldo cobranças em aberto + lançamentos Receita Pendentes avulsos (R1/CA3 — sem dupla contagem por INV-3)
    public decimal Despesas { get; set; }       // despesas pagas
    public decimal Saldo { get; set; }          // Recebido − Despesas

    // KPIs secundários (R1)
    public decimal DescontosConcedidos { get; set; }   // SUM(cobrancas.desconto) do período
    public decimal TaxasCartao { get; set; }           // SUM(pagamentos.taxa) do período
    public decimal Estornos { get; set; }               // valor absoluto dos estornos do período
}
