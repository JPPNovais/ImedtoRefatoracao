using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Termos.Events;

/// <summary>
/// Disparado quando o paciente recusa o termo via link público (Fase 4).
/// Usado para notificar o emissor por e-mail.
/// </summary>
public record TermoRecusadoEvent(
    long TermoEmitidoId,
    long PacienteId,
    long EstabelecimentoId,
    DateTime RecusadoEm) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
