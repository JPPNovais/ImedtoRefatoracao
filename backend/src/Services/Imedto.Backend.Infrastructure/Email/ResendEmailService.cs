using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _apiKey;

    public ResendEmailService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<ResendEmailService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _apiKey = config["Email:ResendApiKey"]
            ?? throw new InvalidOperationException("Email:ResendApiKey não configurado.");
    }

    public async Task EnviarAsync(string para, string assunto, string corpoHtml, CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient("Resend");

        var payload = new
        {
            from    = "Imedto <contato@imedto.com>",
            to      = new[] { para },
            subject = assunto,
            html    = corpoHtml,
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload),
        };
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Resend retornou {Status}: {Body}", (int)resp.StatusCode, body);
            throw new InvalidOperationException($"Falha ao enviar email: {resp.StatusCode}");
        }
    }
}
