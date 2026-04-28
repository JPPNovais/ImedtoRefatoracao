using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Orcamentos.Events;

public record OrcamentoAprovadoEvent(
    long OrcamentoId,
    long EstabelecimentoId,
    long PacienteId,
    decimal Total) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
