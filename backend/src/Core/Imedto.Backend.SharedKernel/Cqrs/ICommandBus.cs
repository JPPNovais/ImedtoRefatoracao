namespace Imedto.Backend.SharedKernel.Cqrs;

/// <summary>
/// Bus para envio de commands. Injetado nos controllers.
/// </summary>
public interface ICommandBus
{
    void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand;
    Task Send<TCommand>(TCommand command) where TCommand : ICommand;
}
