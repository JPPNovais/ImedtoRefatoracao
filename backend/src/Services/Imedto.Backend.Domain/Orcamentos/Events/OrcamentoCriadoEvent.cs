using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Orcamentos.Events;

public record OrcamentoCriadoEvent(
    long OrcamentoId,
    long EstabelecimentoId,
    long PacienteId,
    string Numero) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
