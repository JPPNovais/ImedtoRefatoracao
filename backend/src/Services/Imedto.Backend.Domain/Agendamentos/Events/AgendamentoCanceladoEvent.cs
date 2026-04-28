using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Agendamentos.Events;

public record AgendamentoCanceladoEvent(
    long AgendamentoId,
    long EstabelecimentoId,
    string Motivo) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
