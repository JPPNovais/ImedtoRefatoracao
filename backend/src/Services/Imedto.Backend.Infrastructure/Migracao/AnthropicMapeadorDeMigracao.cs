using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Adapter Anthropic para inferência de mapeamento de colunas (IMapeadorDeMigracao).
/// Adapter puro — sem lógica de negócio. Apenas adaptação HTTP.
/// 
/// Usa o client "Anthropic" já registrado no IHttpClientFactory.
/// Configuração: Ia:AnthropicApiKey e Ia:Modelo em appsettings.
/// </summary>
public sealed class AnthropicMapeadorDeMigracao : IMapeadorDeMigracao
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly string _apiKey;
    private readonly string _modelo;
    private readonly ILogger<AnthropicMapeadorDeMigracao> _logger;

    public AnthropicMapeadorDeMigracao(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<AnthropicMapeadorDeMigracao> logger)
    {
        _httpFactory = httpFactory;
        _apiKey = config["Ia:AnthropicApiKey"] ?? string.Empty;
        _modelo = config["Ia:Modelo"] ?? "claude-haiku-4-5-20251001";
        _logger = logger;
    }

    public async Task<PropostaDeMapa> InferirMapaAsync(
        EsquemaDeArquivo esquema,
        string entidadeAlvo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("IA não configurada. Defina Ia:AnthropicApiKey.");

        var prompt = MontarPrompt(esquema, entidadeAlvo);

        var body = JsonSerializer.Serialize(new
        {
            model = _modelo,
            max_tokens = 1024,
            stream = false,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = prompt } }
        });

        using var client = _httpFactory.CreateClient("Anthropic");
        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        requestMsg.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(requestMsg, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "[Mapeador] Anthropic API erro {Status} ao mapear entidade {Entidade}.",
                response.StatusCode, entidadeAlvo);
            throw new InvalidOperationException($"Erro na API de IA: {response.StatusCode}");
        }

        var respJson = await response.Content.ReadAsStringAsync(ct);
        return ParsearResposta(respJson);
    }

    private static string MontarPrompt(EsquemaDeArquivo esquema, string entidadeAlvo)
    {
        var cabecalhos = string.Join(", ", esquema.Cabecalhos);
        var amostra = esquema.AmostraMascarada.Count > 0
            ? JsonSerializer.Serialize(esquema.AmostraMascarada.Take(5))
            : "(sem amostra disponível)";

        return $$"""
            Você é um especialista em migração de dados para sistemas de saúde brasileiros.

            Arquivo de origem — entidade alvo: **{{entidadeAlvo}}**

            Colunas encontradas no arquivo:
            {{cabecalhos}}

            Amostra de até 5 linhas (dados mascarados):
            {{amostra}}

            Mapeie cada coluna do arquivo para o campo canônico correspondente no sistema Imedto.
            Campos canônicos disponíveis para "{{entidadeAlvo}}":
            - paciente: nome, cpf, data_nascimento, sexo, telefone, email, endereco, cidade, estado, cep, rg, profissao, estado_civil, nome_mae, observacoes
            - agendamento: data_hora, profissional_nome, paciente_nome, tipo_consulta, status, duracao_minutos, sala, observacoes
            - prontuario: data, profissional_nome, paciente_nome, tipo_evolucao, conteudo, diagnostico, cid10
            - convenio: nome, codigo_ans, numero_carteirinha, validade, plano

            Retorne APENAS JSON válido, sem markdown, no formato exato:
            {
              "de_para": { "coluna_origem": "campo_canonico" },
              "confianca": 0.95,
              "duvidas": ["coluna_incerta_1", "coluna_incerta_2"]
            }

            - "de_para": mapeamento de cada coluna de origem para um campo canônico. Coluna sem correspondência → valor "ignorar".
            - "confianca": número entre 0 e 1 representando sua confiança global.
            - "duvidas": lista de colunas com baixa confiança ou ambíguas.
            """;
    }

    private static PropostaDeMapa ParsearResposta(string respJson)
    {
        using var doc = JsonDocument.Parse(respJson);
        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        // Extrai JSON do conteúdo (a IA pode retornar texto antes/depois).
        var inicio = content.IndexOf('{');
        var fim = content.LastIndexOf('}');
        if (inicio < 0 || fim < inicio)
            return Fallback();

        var jsonLimpo = content[inicio..(fim + 1)];

        try
        {
            var parsed = JsonSerializer.Deserialize<MapeamentoIaResponse>(jsonLimpo, JsonOpts);
            if (parsed is null) return Fallback();

            return new PropostaDeMapa
            {
                DeParaColunas = parsed.DePara ?? new Dictionary<string, string>(),
                Confianca = Math.Clamp(parsed.Confianca, 0.0, 1.0),
                Duvidas = parsed.Duvidas ?? [],
            };
        }
        catch
        {
            return Fallback();
        }
    }

    private static PropostaDeMapa Fallback() => new()
    {
        DeParaColunas = new Dictionary<string, string>(),
        Confianca = 0.0,
        Duvidas = [],
    };

    private const string SystemPrompt =
        "Você é um especialista em migração de dados para sistemas de saúde. " +
        "Responda APENAS com JSON válido, sem explicações adicionais.";

    // DTO interno para desserializar resposta da IA.
    private sealed class MapeamentoIaResponse
    {
        [JsonPropertyName("de_para")]
        public Dictionary<string, string>? DePara { get; set; }

        [JsonPropertyName("confianca")]
        public double Confianca { get; set; }

        [JsonPropertyName("duvidas")]
        public List<string>? Duvidas { get; set; }
    }
}
