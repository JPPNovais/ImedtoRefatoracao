using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Commands;

/// <summary>
/// Registra um ou mais pagamentos para uma cobrança (R11 — múltiplas formas).
/// Cada item de Formas gera 1 Pagamento + 1 Lancamento atomicamente.
/// </summary>
public class RegistrarPagamentosCommand : ICommand
{
    public long CobrancaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioId { get; set; }
    /// <summary>Desconto aplicado à cobrança (0 = sem desconto). Só com permissão (INV-8).</summary>
    public decimal Desconto { get; set; }
    public bool PodeAplicarDesconto { get; set; }
    public List<FormaPagamentoItem> Formas { get; set; } = new();
    public DateOnly DataPagamento { get; set; }
}

public class FormaPagamentoItem
{
    public long FormaPagamentoId { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; } = 1;
    public decimal Juros { get; set; }
}
