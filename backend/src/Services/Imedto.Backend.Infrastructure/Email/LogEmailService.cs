using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Email;

public class LogEmailService : IEmailService
{
    private readonly ILogger<LogEmailService> _logger;

    public LogEmailService(ILogger<LogEmailService> logger) => _logger = logger;

    public Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] Para={Para} | Assunto={Assunto}", para, assunto);
        return Task.CompletedTask;
    }
}
