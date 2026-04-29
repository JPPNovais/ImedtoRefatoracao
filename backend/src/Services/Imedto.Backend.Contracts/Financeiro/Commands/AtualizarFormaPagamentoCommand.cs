using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class AtualizarFormaPagamentoCommand : ICommand
{
    public long FormaPagamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
}
