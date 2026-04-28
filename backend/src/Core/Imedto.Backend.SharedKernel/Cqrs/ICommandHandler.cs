namespace Imedto.Backend.SharedKernel.Cqrs;

/// <summary>
/// Handler de command CQRS.
/// Registre no Container: commandBus.Register&lt;MeuCommand, MeuCommandHandler&gt;()
/// </summary>
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command);
}
