using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Prontuarios.Events;

public record EvolucaoRegistradaEvent(long EvolucaoId, long ProntuarioId, Guid AutorUsuarioId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
