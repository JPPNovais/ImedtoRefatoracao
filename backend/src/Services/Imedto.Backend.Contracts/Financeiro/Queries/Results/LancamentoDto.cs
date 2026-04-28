namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

public class LancamentoDto
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public long? OrcamentoId { get; set; }
    public string? OrcamentoNumero { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public class ResumoFinanceiroDto
{
    public decimal TotalReceitasPagas { get; set; }
    public decimal TotalDespesasPagas { get; set; }
    public decimal Saldo { get; set; }
    public decimal ReceitasPendentes { get; set; }
    public decimal DespesasPendentes { get; set; }
}
