using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Commands;

/// <summary>Cria ou atualiza taxa de cartão para uma forma de pagamento.</summary>
public class SalvarConfigTaxaFormaPagamentoCommand : ICommand
{
    /// <summary>null = criar novo; valor > 0 = atualizar existente.</summary>
    public long? Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long FormaPagamentoId { get; set; }
    public decimal TaxaPercentual { get; set; }
    public bool Ativo { get; set; } = true;
}
