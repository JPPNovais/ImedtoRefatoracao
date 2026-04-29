using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class CriarFormaPagamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
}
