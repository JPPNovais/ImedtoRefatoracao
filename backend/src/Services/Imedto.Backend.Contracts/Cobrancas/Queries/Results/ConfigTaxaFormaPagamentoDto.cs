namespace Imedto.Backend.Contracts.Cobrancas.Queries.Results;

public class ConfigTaxaFormaPagamentoDto
{
    public long Id { get; set; }
    public long FormaPagamentoId { get; set; }
    public string FormaPagamentoNome { get; set; } = string.Empty;
    public decimal TaxaPercentual { get; set; }
    public bool Ativo { get; set; }
}
