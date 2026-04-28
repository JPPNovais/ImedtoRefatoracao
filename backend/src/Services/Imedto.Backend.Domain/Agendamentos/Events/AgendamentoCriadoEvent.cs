using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Agendamentos.Events;

public record AgendamentoCriadoEvent(
    long AgendamentoId,
    long EstabelecimentoId,
    long PacienteId,
    Guid ProfissionalUsuarioId,
    DateTime InicioPrevisto) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
