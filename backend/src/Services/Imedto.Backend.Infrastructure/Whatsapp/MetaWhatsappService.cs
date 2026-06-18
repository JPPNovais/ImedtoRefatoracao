using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Whatsapp;

/// <summary>
/// Implementação real do <see cref="IWhatsappService"/> via Meta WhatsApp Cloud API
/// (Graph API v19.0). Envia mensagens de template aprovado (categoria Utility).
///
/// Configuração (appsettings / SSM):
///   <c>Whatsapp:AccessToken</c>   — token de acesso permanente do app Meta
///   <c>Whatsapp:PhoneNumberId</c> — ID do número de telefone do remetente no WABA
///   <c>Whatsapp:TemplateName</c>  — nome do template aprovado (default: lembrete_consulta)
///   <c>Whatsapp:BaseUrl</c>       — base URL da Graph API (default: https://graph.facebook.com/v19.0)
///
/// LGPD: logs nunca incluem número de telefone, nome, variáveis ou corpo.
/// Apenas hash SHA-256 truncado do destinatário para correlação.
/// </summary>
public class MetaWhatsappService : IWhatsappService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MetaWhatsappService> _logger;
    private readonly string _accessToken;
    private readonly string _phoneNumberId;
    private readonly string _templateName;

    public MetaWhatsappService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<MetaWhatsappService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;

        _accessToken = config["Whatsapp:AccessToken"]
            ?? throw new InvalidOperationException("Whatsapp:AccessToken não configurado.");
        _phoneNumberId = config["Whatsapp:PhoneNumberId"]
            ?? throw new InvalidOperationException("Whatsapp:PhoneNumberId não configurado.");
        _templateName = config["Whatsapp:TemplateName"] ?? "lembrete_consulta";
    }

    public async Task EnviarTemplateAsync(
        string para,
        IReadOnlyList<string> variaveis,
        CancellationToken ct = default)
    {
        var hashDestinatario = Hash(para);

        // Monta os parâmetros do corpo do template — cada variável {{N}} vira um
        // objeto { "type": "text", "text": "valor" } na ordem do array.
        var parametros = (variaveis ?? Array.Empty<string>())
            .Select(v => new { type = "text", text = v })
            .ToArray();

        // Payload conforme a Graph API v19.0 — messages endpoint.
        // O SDK da Meta não é usado aqui (premissa ports & adapters).
        var payload = new
        {
            messaging_product = "whatsapp",
            to = para,
            type = "template",
            template = new
            {
                name = _templateName,
                language = new { code = "pt_BR" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = parametros
                    }
                }
            }
        };

        var client = _httpFactory.CreateClient("MetaWhatsapp");

        try
        {
            using var req = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_phoneNumberId}/messages")
            {
                Content = JsonContent.Create(payload),
            };
            req.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var resp = await client.SendAsync(req, ct);

            if (resp.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[MetaWhatsapp] Template enviado com sucesso (hash destinatário {Hash}).",
                    hashDestinatario);
                return;
            }

            var status = (int)resp.StatusCode;
            var body = await SafeLerCorpoAsync(resp, ct);

            // Qualquer erro da API Meta é tratado como falha de entrega — o caller
            // (handler do job) decide se silencia ou faz retry na próxima rodada.
            _logger.LogWarning(
                "[MetaWhatsapp] Falha ao enviar template (HTTP {Status}, hash {Hash}). Erro: {Body}",
                status, hashDestinatario, body);

            // Propaga para o caller saber que não deve marcar como enviado.
            throw new InvalidOperationException($"Meta API retornou HTTP {status}.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "[MetaWhatsapp] Erro de rede ao chamar a Graph API (hash {Hash}).",
                hashDestinatario);
            throw;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                "[MetaWhatsapp] Timeout ao chamar a Graph API (hash {Hash}).",
                hashDestinatario);
            throw;
        }
    }

    private static string Hash(string s)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s ?? string.Empty)))[..12];

    private static async Task<string> SafeLerCorpoAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try { return await resp.Content.ReadAsStringAsync(ct); }
        catch { return string.Empty; }
    }
}
