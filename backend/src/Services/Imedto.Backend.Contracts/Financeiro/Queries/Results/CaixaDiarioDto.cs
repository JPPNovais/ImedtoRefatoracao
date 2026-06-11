namespace Imedto.Backend.Contracts.Financeiro.Queries.Results;

/// <summary>DTO do caixa diário com resumo on-the-fly (R8).</summary>
public class CaixaDiarioDto
{
    public long Id { get; set; }
    public DateOnly Data { get; set; }
    public string Status { get; set; } = string.Empty; // Aberto | Fechado

    public Guid AbertoPorUsuarioId { get; set; }
    public string AbertoPorNome { get; set; } = string.Empty;
    public DateTime AbertoEm { get; set; }

    public Guid? FechadoPorUsuarioId { get; set; }
    public string? FechadoPorNome { get; set; }
    public DateTime? FechadoEm { get; set; }
    public string? Observacao { get; set; }

    public Guid? ReabertoPorUsuarioId { get; set; }
    public DateTime? ReabertoEm { get; set; }

    /// <summary>Resumo do dia por forma de pagamento (calculado on-the-fly de Lancamento — R8).</summary>
    public IEnumerable<ResumoCaixaFormaPagamentoDto> ResumoPorForma { get; set; }
        = Array.Empty<ResumoCaixaFormaPagamentoDto>();

    public decimal TotalDia { get; set; }
    public decimal TotalEstornos { get; set; }
}

public class ResumoCaixaFormaPagamentoDto
{
    public string FormaPagamento { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
