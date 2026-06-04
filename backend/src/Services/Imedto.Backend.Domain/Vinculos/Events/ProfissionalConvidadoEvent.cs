using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Vinculos.Events;

public record ProfissionalConvidadoEvent(
    long VinculoId,
    Guid ProfissionalUsuarioId,
    long EstabelecimentoId,
    Guid ConvidadoPorUsuarioId,
    string? MensagemPersonalizada = null) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
