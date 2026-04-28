using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Pacientes.Events;

public record PacienteCadastradoEvent(long PacienteId, long EstabelecimentoId, string NomeCompleto) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
