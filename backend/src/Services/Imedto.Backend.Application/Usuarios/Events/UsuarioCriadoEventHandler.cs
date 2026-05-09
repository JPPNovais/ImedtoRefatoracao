using System.Security.Cryptography;
using System.Text;
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
        // LGPD: hash do email em log estruturado (não logar email em claro).
        // O Id ja correlaciona o usuario; o hash permite ver duplicidade sem expor PII.
        _logger.LogInformation(
            "Usuário criado: Id={UsuarioId}, EmailHash={EmailHash}, OcorridoEm={OcorridoEm}",
            domainEvent.UsuarioId, HashEmail(domainEvent.Email), domainEvent.OcorridoEm);
        return Task.CompletedTask;
    }

    private static string HashEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "(vazio)";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant())))[..16];
    }
}
