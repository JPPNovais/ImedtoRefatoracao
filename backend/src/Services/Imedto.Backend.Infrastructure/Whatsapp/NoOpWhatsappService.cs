using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Whatsapp;

/// <summary>
/// Implementação fallback do <see cref="IWhatsappService"/> usada em dev/teste quando
/// <c>Whatsapp:Provider</c> não está configurado ou as credenciais estão ausentes.
/// Apenas loga que a mensagem seria enviada, SEM destinatário em texto puro
/// (LGPD — apenas hash SHA-256 truncado do número de telefone).
/// </summary>
public class NoOpWhatsappService : IWhatsappService
{
    private readonly ILogger<NoOpWhatsappService> _logger;

    public NoOpWhatsappService(ILogger<NoOpWhatsappService> logger) => _logger = logger;

    public Task EnviarTemplateAsync(
        string para,
        IReadOnlyList<string> variaveis,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NoOpWhatsapp] Mensagem de template seria enviada (hash destinatário {Hash}, {Qtd} variável(is)).",
            Hash(para), variaveis?.Count ?? 0);
        return Task.CompletedTask;
    }

    private static string Hash(string s)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s ?? string.Empty)))[..12];
}
