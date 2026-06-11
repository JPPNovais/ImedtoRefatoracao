namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

/// <summary>
/// Lançamento no extrato do /financeiro (R2/R3).
/// Inclui paciente/cobrança quando a origem é pagamento (LGPD: nome só quando há vínculo de cobrança).
/// </summary>
public class LancamentoExtratoDto
{
    public long Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? FormaPagamento { get; set; }
    public string? Origem { get; set; }
    public long? CobrancaId { get; set; }
    /// <summary>Preenchido apenas quando CobrancaId != null (R3/LGPD).</summary>
    public long? PacienteId { get; set; }
    public string? PacienteNome { get; set; }
    public string CriadoPorNome { get; set; } = string.Empty;
}

public class PaginaLancamentosExtratoDto
{
    public IEnumerable<LancamentoExtratoDto> Itens { get; set; } = Array.Empty<LancamentoExtratoDto>();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
}
