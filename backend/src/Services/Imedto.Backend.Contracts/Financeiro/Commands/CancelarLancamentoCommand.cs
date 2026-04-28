using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class CancelarLancamentoCommand : ICommand
{
    public long LancamentoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
