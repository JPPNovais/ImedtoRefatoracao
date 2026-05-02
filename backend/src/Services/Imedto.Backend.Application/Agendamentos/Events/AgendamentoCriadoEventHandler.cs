using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Agendamentos.Events;

public class AgendamentoCriadoEventHandler : IEventHandler<AgendamentoCriadoEvent>
{
    public Task Handle(AgendamentoCriadoEvent domainEvent) => Task.CompletedTask;
}
