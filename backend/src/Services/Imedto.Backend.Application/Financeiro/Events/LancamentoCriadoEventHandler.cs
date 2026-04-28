using Imedto.Backend.Domain.Financeiro.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Financeiro.Events;

public class LancamentoCriadoEventHandler : IEventHandler<LancamentoCriadoEvent>
{
    public Task Handle(LancamentoCriadoEvent @event) => Task.CompletedTask;
}
