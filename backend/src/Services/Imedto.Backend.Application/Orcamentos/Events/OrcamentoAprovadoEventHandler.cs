using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Orcamentos.Events;

// Placeholder — integração financeira futura será implementada aqui.
public class OrcamentoAprovadoEventHandler : IEventHandler<OrcamentoAprovadoEvent>
{
    public Task Handle(OrcamentoAprovadoEvent domainEvent) => Task.CompletedTask;
}
