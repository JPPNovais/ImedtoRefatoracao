using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Termos.Events;

/// <summary>
/// Disparado após emitir e persistir um <see cref="TermoEmitido"/>. Pode ser ouvido por
/// engine de automações ou por audit trail dedicado.
/// </summary>
public record TermoEmitidoEvent(
    long TermoEmitidoId,
    long PacienteId,
    long EstabelecimentoId,
    long TermoModeloId,
    Guid EmitidoPorUsuarioId,
    AssinaturaTipo AssinaturaTipo) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
