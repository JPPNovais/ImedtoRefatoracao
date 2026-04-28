using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Vinculos.Events;

public record VinculoAceitoEvent(long VinculoId, Guid ProfissionalUsuarioId, long EstabelecimentoId) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
