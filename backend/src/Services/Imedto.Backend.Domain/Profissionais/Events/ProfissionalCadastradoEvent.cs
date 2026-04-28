using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Profissionais.Events;

public record ProfissionalCadastradoEvent(Guid UsuarioId, string Conselho, string Uf, string NumeroRegistro) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
