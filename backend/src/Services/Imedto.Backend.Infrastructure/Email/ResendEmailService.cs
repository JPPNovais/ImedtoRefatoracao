using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Email;

/// <summary>
/// Implementação real do <see cref="IEmailService"/> via API Resend (https://resend.com).
/// API-first, sem SDK proprietário — usamos <see cref="HttpClient"/> direto.
///
/// Resiliência:
///   * 4xx → erro permanente (não retry, log de erro sem PII).
///   * 5xx / falha de rede → retry com backoff exponencial (3 tentativas: 200ms, 400ms, 800ms).
///
/// LGPD:
///   * Logs nunca incluem destinatário, assunto ou corpo. Apenas hash SHA-256
///     truncado do destinatário para correlação.
/// </summary>
public class ResendEmailService : IEmailService
{
    private const int TentativasMax = 3;
    private const int BaseBackoffMs = 200;

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _from;

    public ResendEmailService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<ResendEmailService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;

        // Aceita Email:ApiKey (novo, item 4.7) com fallback para Email:ResendApiKey (legado).
        _apiKey = config["Email:ApiKey"]
            ?? config["Email:ResendApiKey"]
            ?? throw new InvalidOperationException("Email:ApiKey não configurado.");

        _from = config["Email:From"] ?? "Imedto <noreply@imedto.com.br>";
    }

    public Task EnviarAsync(
        string para,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default)
        => EnviarInternoAsync(new[] { para }, assunto, corpoHtml, corpoTexto, ct);

    public Task EnviarMultiplosAsync(
        IEnumerable<string> paraLista,
        string assunto,
        string corpoHtml,
        string? corpoTexto = null,
        CancellationToken ct = default)
    {
        var destinatarios = paraLista?.Where(e => !string.IsNullOrWhiteSpace(e)).ToArray()
            ?? Array.Empty<string>();
        if (destinatarios.Length == 0) return Task.CompletedTask;
        return EnviarInternoAsync(destinatarios, assunto, corpoHtml, corpoTexto, ct);
    }

    private async Task EnviarInternoAsync(
        string[] destinatarios,
        string assunto,
        string corpoHtml,
        string? corpoTexto,
        CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("Resend");

        // Body conforme docs do Resend: from, to (array), subject, html, text (opcional).
        var payload = new
        {
            from = _from,
            to = destinatarios,
            subject = assunto,
            html = corpoHtml,
            text = corpoTexto
        };

        var hashCorrelacao = HashCorrelacao(destinatarios);

        for (var tentativa = 1; tentativa <= TentativasMax; tentativa++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "emails")
                {
                    Content = JsonContent.Create(payload),
                };
                req.Headers.Add("Authorization", $"Bearer {_apiKey}");

                var resp = await client.SendAsync(req, ct);

                if (resp.IsSuccessStatusCode) return;

                var status = (int)resp.StatusCode;

                // 4xx (exceto 408/429): erro permanente — não adianta retry.
                if (status >= 400 && status < 500
                    && resp.StatusCode != HttpStatusCode.RequestTimeout
                    && resp.StatusCode != HttpStatusCode.TooManyRequests)
                {
                    _logger.LogError(
                        "Falha permanente ao enviar email (Resend status {Status}, hash {Hash}).",
                        status, hashCorrelacao);
                    return; // não bloqueia caller; emails não bloqueiam handler
                }

                // 5xx / 408 / 429 → retry.
                _logger.LogWarning(
                    "Falha transitória Resend status {Status} (tentativa {Tentativa}/{Max}, hash {Hash}).",
                    status, tentativa, TentativasMax, hashCorrelacao);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,
                    "Erro de rede ao chamar Resend (tentativa {Tentativa}/{Max}, hash {Hash}).",
                    tentativa, TentativasMax, hashCorrelacao);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Timeout ao chamar Resend (tentativa {Tentativa}/{Max}, hash {Hash}).",
                    tentativa, TentativasMax, hashCorrelacao);
            }

            if (tentativa < TentativasMax)
            {
                var delay = TimeSpan.FromMilliseconds(BaseBackoffMs * Math.Pow(2, tentativa - 1));
                await Task.Delay(delay, ct);
            }
        }

        _logger.LogError(
            "Email não enviado após {Max} tentativas (hash {Hash}).",
            TentativasMax, hashCorrelacao);
    }

    private static string HashCorrelacao(IEnumerable<string> destinatarios)
    {
        var concat = string.Join(';', destinatarios);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(concat)))[..16];
    }
}
