using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class InativarFormaPagamentoCommand : ICommand
{
    public long FormaPagamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
