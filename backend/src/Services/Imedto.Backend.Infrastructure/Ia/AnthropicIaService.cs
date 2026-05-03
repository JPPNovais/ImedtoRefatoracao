using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Ia;

public class AnthropicIaService : IIaService
{
    private const string SystemPrompt =
        """
        Você é um assistente médico especializado na redação de prontuários clínicos.
        Preencha a seção solicitada com base no contexto fornecido.
        Use linguagem médica profissional em português brasileiro.
        Seja objetivo, conciso e clínico.
        Não invente diagnósticos, medicamentos ou procedimentos não mencionados.
        Gere apenas o conteúdo da seção, sem repetir o título.
        Esta é uma sugestão que deve ser revisada e aprovada pelo profissional responsável.
        """;

    private readonly IHttpClientFactory _httpFactory;
    private readonly string _apiKey;
    private readonly string _modelo;
    private readonly ILogger<AnthropicIaService> _logger;

    public AnthropicIaService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<AnthropicIaService> logger)
    {
        _httpFactory = httpFactory;
        _apiKey = config["Ia:AnthropicApiKey"] ?? string.Empty;
        _modelo = config["Ia:Modelo"] ?? "claude-haiku-4-5-20251001";
        _logger = logger;
    }

    public async IAsyncEnumerable<string> SugerirSecaoProntuarioAsync(
        SugestaoSecaoProntuarioRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("IA não configurada. Defina Ia:AnthropicApiKey nas configurações.");

        var contexto = request.SecoesContexto.Count > 0
            ? string.Join("\n", request.SecoesContexto.Select(kv => $"- {kv.Key}: {kv.Value}"))
            : "(nenhum contexto adicional fornecido)";

        var prompt = $"""
            Preencha a seção "{request.SecaoAlvoTitulo}" do prontuário clínico.

            Contexto das outras seções já preenchidas:
            {contexto}

            Gere apenas o conteúdo para "{request.SecaoAlvoTitulo}":
            """;

        var body = JsonSerializer.Serialize(new
        {
            model = _modelo,
            max_tokens = 1024,
            stream = true,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = prompt } }
        });

        using var client = _httpFactory.CreateClient("Anthropic");
        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        requestMsg.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(
            requestMsg, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Anthropic API erro {Status}: {Body}", response.StatusCode, err);
            throw new InvalidOperationException($"Erro na API de IA: {response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        // Antes usavamos reader.EndOfStream (CA2024) — isso eh um peek sincrono que
        // pode bloquear a thread em stream de rede. Iteramos por ReadLineAsync que ja
        // sinaliza fim via null retornado.
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") break;

            string? chunk = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var type) &&
                    type.GetString() == "content_block_delta" &&
                    root.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("type", out var deltaType) &&
                    deltaType.GetString() == "text_delta" &&
                    delta.TryGetProperty("text", out var text))
                {
                    chunk = text.GetString();
                }
            }
            catch (JsonException) { }

            if (!string.IsNullOrEmpty(chunk))
                yield return chunk;
        }
    }
}
