using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Lgpd.Commands;

public class AnonimizarMinhaContaCommand : ICommand
{
    public Guid UsuarioId { get; init; }
}
