using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Termos.Events;

/// <summary>
/// Disparado quando o termo entra em estado <see cref="StatusTermoEmitido.Assinado"/> —
/// seja por upload de PDF (emissor) ou aceite via link público (paciente).
/// </summary>
public record TermoAssinadoEvent(
    long TermoEmitidoId,
    long PacienteId,
    long EstabelecimentoId,
    AssinaturaTipo AssinaturaTipo,
    DateTime AssinadoEm) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
