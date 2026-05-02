using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Usuarios.Events;

/// <summary>
/// Handler inicial de <see cref="UsuarioCriadoEvent"/>. Mantido simples por enquanto —
/// nas próximas fases pode disparar e-mail de boas-vindas, criar preferências default, etc.
/// </summary>
public class UsuarioCriadoEventHandler : IEventHandler<UsuarioCriadoEvent>
{
    private readonly ILogger<UsuarioCriadoEventHandler> _logger;

    public UsuarioCriadoEventHandler(ILogger<UsuarioCriadoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UsuarioCriadoEvent domainEvent)
    {
        _logger.LogInformation(
            "Usuário criado: Id={UsuarioId}, Email={Email}, OcorridoEm={OcorridoEm}",
            domainEvent.UsuarioId, domainEvent.Email, domainEvent.OcorridoEm);
        return Task.CompletedTask;
    }
}
