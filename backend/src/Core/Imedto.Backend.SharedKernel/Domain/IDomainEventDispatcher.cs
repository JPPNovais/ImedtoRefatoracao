using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.SharedKernel.Domain;

/// <summary>
/// Coleta eventos de dominio de aggregates rastreados (via ChangeTracker)
/// e os publica via <see cref="IEventBus"/>. Acionado automaticamente pelo
/// <c>EfUnitOfWorkScope</c> apos <c>SaveChangesAsync</c> e antes do
/// <c>transaction.Commit</c> — garantindo que eventos veem o estado ja
/// persistido (Ids gerados, etc.).
///
/// Idempotente: aggregate cujo <c>DomainEvents</c> ja foi limpado pelo
/// handler manualmente nao gera duplicacao.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Coleta eventos de aggregates rastreados, publica via IEventBus e
    /// chama ClearDomainEvents em cada aggregate. Roda dentro da transacao
    /// do UoW — falha de handler de evento aborta o commit.
    /// </summary>
    Task DispatchAsync();
}
