using Imedto.Backend.Domain.Financeiro.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Events;

public class LancamentoPagoEventHandler : IEventHandler<LancamentoPagoEvent>
{
    public Task Handle(LancamentoPagoEvent @event) => Task.CompletedTask;
}
