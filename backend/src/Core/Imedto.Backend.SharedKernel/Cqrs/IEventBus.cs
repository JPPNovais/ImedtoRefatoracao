namespace Imedto.Backend.SharedKernel.Cqrs;

/// <summary>
/// Bus para publicação de domain events.
/// </summary>
public interface IEventBus
{
    void Register<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;
    Task Publish<TEvent>(TEvent @event) where TEvent : IDomainEvent;
}
