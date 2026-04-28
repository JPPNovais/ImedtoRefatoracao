using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Prontuarios.Events;

public record ProntuarioIniciadoEvent(long ProntuarioId, long PacienteId, long EstabelecimentoId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
