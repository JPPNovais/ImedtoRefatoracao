namespace Imedto.Backend.SharedKernel.Cqrs;

/// <summary>
/// Handler de domain event.
/// Pode ser registrado no bus local (síncrono) e/ou no bus distribuído (Kafka/MassTransit).
/// </summary>
public interface IEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent @event);
}
