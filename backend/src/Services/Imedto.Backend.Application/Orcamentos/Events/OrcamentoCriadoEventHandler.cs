using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Orcamentos.Events;

// Placeholder — notificações futuras ao paciente serão implementadas aqui.
public class OrcamentoCriadoEventHandler : IEventHandler<OrcamentoCriadoEvent>
{
    public Task Handle(OrcamentoCriadoEvent @event) => Task.CompletedTask;
}
