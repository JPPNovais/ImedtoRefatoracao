using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class ExcluirVariavelPoolCommand : ICommand
{
    public long ItemId { get; set; }
    public long EstabelecimentoId { get; set; }
}
