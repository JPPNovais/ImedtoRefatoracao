using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Domain.Usuarios.Events;

/// <summary>
/// Domain event emitido quando um novo usuário é criado na plataforma
/// (após signup no Supabase Auth).
/// </summary>
public record UsuarioCriadoEvent(Guid UsuarioId, string Email) : IDomainEvent
{
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
