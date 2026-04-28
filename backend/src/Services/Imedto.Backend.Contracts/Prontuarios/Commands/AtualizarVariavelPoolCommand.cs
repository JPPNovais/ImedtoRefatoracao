using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class AtualizarVariavelPoolCommand : ICommand
{
    public long ItemId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; }
}
