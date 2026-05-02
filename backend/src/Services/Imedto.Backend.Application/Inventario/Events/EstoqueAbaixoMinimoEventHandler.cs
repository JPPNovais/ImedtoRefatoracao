using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Inventario.Events;

// Placeholder — futuras notificações (push, e-mail) serão implementadas aqui.
public class EstoqueAbaixoMinimoEventHandler : IEventHandler<EstoqueAbaixoMinimoEvent>
{
    public Task Handle(EstoqueAbaixoMinimoEvent domainEvent) => Task.CompletedTask;
}
