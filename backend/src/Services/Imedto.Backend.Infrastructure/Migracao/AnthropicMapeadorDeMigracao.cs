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
/// Addendum 4 (CA73-CA75):
///   - InferirBlocoAsync: 1 chamada por bloco que classifica a entidade E mapeia as colunas (D-N2).
///   - InferirMapaAsync: mantido para compatibilidade; internamente usa InferirBlocoAsync.
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

    // Lista canônica usada no prompt (D-S1).
    private static readonly string EntidadesParaPrompt = string.Join(", ",
        EntidadesCanônicas.Paciente,
        EntidadesCanônicas.Agendamento,
        EntidadesCanônicas.FornecedorEstoque,
        EntidadesCanônicas.CategoriaEstoque,
        EntidadesCanônicas.FabricanteEstoque,
        EntidadesCanônicas.LocalEstoque,
        EntidadesCanônicas.ItemEstoque,
        EntidadesCanônicas.ProdutoOrcamento,
        EntidadesCanônicas.ProcedimentoOrcamento,
        EntidadesCanônicas.Prontuario,
        EntidadesCanônicas.SemEquivalente);

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

    // ─── InferirBlocoAsync (CA73-CA75, D-N2) ─────────────────────────────────

    public async Task<PropostaDeBlocoMapeado> InferirBlocoAsync(
        EsquemaDeArquivo esquema,
        string hintNome,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("IA não configurada. Defina Ia:AnthropicApiKey.");

        var prompt = MontarPromptBloco(esquema, hintNome);

        var body = JsonSerializer.Serialize(new
        {
            model = _modelo,
            max_tokens = 1024,
            stream = false,
            system = SystemPromptBloco,
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
                "[Mapeador] Anthropic API erro {Status} ao classificar bloco '{Hint}'.",
                response.StatusCode, hintNome);
            throw new InvalidOperationException($"Erro na API de IA: {response.StatusCode}");
        }

        var respJson = await response.Content.ReadAsStringAsync(ct);
        return ParsearRespostaBloco(respJson);
    }

    // ─── InferirMapaAsync (compatibilidade — chama InferirBlocoAsync internamente) ──

    public async Task<PropostaDeMapa> InferirMapaAsync(
        EsquemaDeArquivo esquema,
        string entidadeAlvo,
        CancellationToken ct = default)
    {
        // Compatibilidade: usa entidadeAlvo como hint e descarta a classificação automática.
        // O de-para retornado respeita o contrato original.
        var proposta = await InferirBlocoAsync(esquema, entidadeAlvo, ct);
        return new PropostaDeMapa
        {
            DeParaColunas = proposta.DeParaColunas,
            Confianca = proposta.Confianca,
            Duvidas = proposta.Duvidas,
        };
    }

    // ─── Prompts ──────────────────────────────────────────────────────────────

    private static string MontarPromptBloco(EsquemaDeArquivo esquema, string hintNome)
    {
        var cabecalhos = string.Join(", ", esquema.Cabecalhos);
        var amostra = esquema.AmostraMascarada.Count > 0
            ? JsonSerializer.Serialize(esquema.AmostraMascarada.Take(5))
            : "(sem amostra disponível)";

        return $$"""
            Você é um especialista em migração de dados para sistemas de saúde brasileiros.

            Bloco de origem (hint de nome: "{{hintNome}}") — dado de um sistema desconhecido.

            Colunas encontradas neste bloco:
            {{cabecalhos}}

            Amostra de até 5 linhas (dados mascarados por privacidade — CPF/nome/telefone ofuscados):
            {{amostra}}

            TAREFA 1 — Classifique este bloco em UMA das entidades canônicas do Imedto:
            {{EntidadesParaPrompt}}

            Use "sem_equivalente" se não houver correspondência clara (ex.: tabelas de repartições, logs de acesso, configurações internas).
            O hint de nome é contexto auxiliar — a classificação deve ser baseada nas COLUNAS e CONTEÚDO, não no nome.

            TAREFA 2 — Mapeie cada coluna para o campo canônico correspondente na entidade classificada.
            Campos canônicos por entidade:
            - paciente: nome, cpf, data_nascimento, sexo, telefone, email, endereco, cidade, estado, cep, rg, profissao, estado_civil, nome_mae, observacoes
            - agendamento: data_hora, profissional_nome, paciente_nome, tipo_consulta, status, duracao_minutos, sala, observacoes
            - prontuario: data, profissional_nome, paciente_nome, tipo_evolucao, conteudo, diagnostico, cid10
            - fornecedor_estoque: nome, cnpj, email, telefone, contato
            - categoria_estoque: nome, descricao
            - fabricante_estoque: nome, cnpj, contato
            - local_estoque: nome, descricao
            - item_estoque: nome, descricao, categoria, fabricante, local, unidade, preco_custo, estoque_minimo
            - produto_orcamento: nome, descricao, preco, categoria
            - procedimento_orcamento: nome, descricao, preco, duracao_minutos

            NUNCA use id interno do sistema de origem como campo canônico (ex.: "id", "paciente_id", "user_id"). Ids externos são ignorados.

            Retorne APENAS JSON válido, sem markdown:
            {
              "entidade_classificada": "paciente",
              "confianca_classificacao": 0.95,
              "de_para": { "coluna_origem": "campo_canonico" },
              "confianca": 0.90,
              "duvidas": ["coluna_incerta"]
            }

            - "entidade_classificada": valor exato da lista canônica (ou "sem_equivalente").
            - "confianca_classificacao": confiança da classificação da entidade (0–1).
            - "de_para": mapeamento de cada coluna para campo canônico; coluna sem correspondência → "ignorar".
            - "confianca": confiança global do mapeamento de colunas (0–1).
            - "duvidas": colunas com baixa confiança.
            """;
    }

    // ─── Parsing de resposta ──────────────────────────────────────────────────

    private static PropostaDeBlocoMapeado ParsearRespostaBloco(string respJson)
    {
        using var doc = JsonDocument.Parse(respJson);
        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        var inicio = content.IndexOf('{');
        var fim = content.LastIndexOf('}');
        if (inicio < 0 || fim < inicio)
            return FallbackBloco();

        var jsonLimpo = content[inicio..(fim + 1)];

        try
        {
            var parsed = JsonSerializer.Deserialize<BlocoIaResponse>(jsonLimpo, JsonOpts);
            if (parsed is null) return FallbackBloco();

            var entidade = parsed.EntidadeClassificada ?? EntidadesCanônicas.SemEquivalente;
            // Valida que a entidade está na lista fechada (D-S1).
            if (!EntidadesCanônicas.EhValida(entidade))
                entidade = EntidadesCanônicas.SemEquivalente;

            return new PropostaDeBlocoMapeado
            {
                EntidadeClassificada = entidade,
                ConfiancaClassificacao = Math.Clamp(parsed.ConfiancaClassificacao, 0.0, 1.0),
                DeParaColunas = parsed.DePara ?? new Dictionary<string, string>(),
                Confianca = Math.Clamp(parsed.Confianca, 0.0, 1.0),
                Duvidas = parsed.Duvidas ?? [],
            };
        }
        catch
        {
            return FallbackBloco();
        }
    }

    private static PropostaDeBlocoMapeado FallbackBloco() => new()
    {
        EntidadeClassificada = EntidadesCanônicas.SemEquivalente,
        ConfiancaClassificacao = 0.0,
        DeParaColunas = new Dictionary<string, string>(),
        Confianca = 0.0,
        Duvidas = [],
    };

    private const string SystemPromptBloco =
        "Você é um especialista em migração de dados para sistemas de saúde brasileiros. " +
        "Responda APENAS com JSON válido, sem explicações adicionais. " +
        "Nunca use ids internos do sistema de origem como campo canônico.";

    // DTO interno para desserializar resposta da IA (bloco classificado + mapeado).
    private sealed class BlocoIaResponse
    {
        [JsonPropertyName("entidade_classificada")]
        public string? EntidadeClassificada { get; set; }

        [JsonPropertyName("confianca_classificacao")]
        public double ConfiancaClassificacao { get; set; }

        [JsonPropertyName("de_para")]
        public Dictionary<string, string>? DePara { get; set; }

        [JsonPropertyName("confianca")]
        public double Confianca { get; set; }

        [JsonPropertyName("duvidas")]
        public List<string>? Duvidas { get; set; }
    }
}
