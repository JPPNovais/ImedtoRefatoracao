using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Marker para aggregates que carregam <see cref="IDomainEvent"/> coletados
/// pelo <see cref="IDomainEventDispatcher"/> ao final do UoW. Implementado
/// pela base <see cref="Entity{TId}"/> automaticamente.
/// </summary>
public interface IDomainEventCarrier
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
