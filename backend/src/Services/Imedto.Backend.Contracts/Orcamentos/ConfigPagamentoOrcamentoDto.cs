namespace Imedto.Backend.Contracts.Orcamentos;

/// <summary>
/// Configuração extra de pagamento (item 7) — schema fechado para substituir o antigo
/// <c>ConfigPagamentoJson</c> opaco. Reside em <c>Contracts</c> para ser usável por
/// commands/DTOs sem que o Contracts dependa de Domain. O handler converte para
/// <c>Domain.Orcamentos.ConfigPagamentoOrcamento</c> antes de chamar a fábrica.
/// </summary>
public class ConfigPagamentoOrcamentoDto
{
    public decimal? DescontoPercentual { get; set; }
    public decimal? DescontoValor { get; set; }
    public decimal? JurosPercentual { get; set; }
    public int? ParcelasMaximas { get; set; }
    public decimal? TaxaParcela { get; set; }
    public string? Observacoes { get; set; }
}
