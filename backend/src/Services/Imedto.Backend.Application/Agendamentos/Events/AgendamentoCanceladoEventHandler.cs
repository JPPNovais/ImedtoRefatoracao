using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Agendamentos.Events;

public class AgendamentoCanceladoEventHandler : IEventHandler<AgendamentoCanceladoEvent>
{
    public Task Handle(AgendamentoCanceladoEvent domainEvent) => Task.CompletedTask;
}
