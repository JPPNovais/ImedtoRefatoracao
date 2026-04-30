using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Email;

/// <summary>
/// Implementação fallback do <see cref="IEmailService"/> usada em dev/teste quando
/// <c>Email:ApiKey</c> não está configurado. Apenas loga que o email seria enviado,
/// SEM corpo, SEM destinatário em texto puro (LGPD — apenas hash SHA-256 truncado).
/// </summary>
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    public Task EnviarAsync(
        string para,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NoOpEmail] Email seria enviado (hash destinatário {Hash}).",
            Hash(para));
        return Task.CompletedTask;
    }

    public Task EnviarMultiplosAsync(
        IEnumerable<string> paraLista,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default)
    {
        var qtd = paraLista?.Count() ?? 0;
        _logger.LogInformation(
            "[NoOpEmail] Lote de {Quantidade} email(s) seria enviado.", qtd);
        return Task.CompletedTask;
    }

    private static string Hash(string s)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s ?? string.Empty)))[..12];
}
