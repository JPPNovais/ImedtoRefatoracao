namespace Imedto.Backend.Contracts.Cobrancas.Queries.Results;

/// <summary>Dados da cobrança para o PaymentModal (CA4/CA5/CA7/CA8/CA12).</summary>
public class CobrancaDetalheDto
{
    public long Id { get; set; }
    public string TipoAtendimento { get; set; } = string.Empty;
    public decimal ValorCobrado { get; set; }
    public decimal Desconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public decimal TotalPago { get; set; }
    public decimal SaldoDevedor { get; set; }
    public string Status { get; set; } = string.Empty;
    public IEnumerable<PagamentoResumoDto> Pagamentos { get; set; } = Array.Empty<PagamentoResumoDto>();
}

public class PagamentoResumoDto
{
    public long Id { get; set; }
    public decimal Valor { get; set; }
    public string FormaPagamentoNome { get; set; } = string.Empty;
    public int Parcelas { get; set; }
    public decimal Taxa { get; set; }
    public DateOnly DataPagamento { get; set; }
}
