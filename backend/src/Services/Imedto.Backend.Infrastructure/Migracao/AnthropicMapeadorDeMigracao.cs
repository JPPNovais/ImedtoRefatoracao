using System.Net;
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
/// Addendum 5 (CA86-CA89):
///   - Retry com backoff exponencial + jitter, respeitando Retry-After.
///   - Teto de 5 tentativas. 4xx≠429 é permanente (não retenta).
///   - 429 e 529/overloaded e falhas de rede são transitórios.
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

    // Retry — espelha ResendEmailService (CA86-CA89/D-R1).
    private const int TentativasMax = 5;
    private const int BaseBackoffMs = 1000; // ~1s inicial (D-R1)

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

        // Client criado fora do loop — não pode ser descartado entre tentativas.
        // IHttpClientFactory gerencia o ciclo de vida do HttpClient internamente.
        var client = _httpFactory.CreateClient("Anthropic");

        // Retry com backoff exponencial + jitter (CA86-CA89/D-R1).
        // 4xx≠429 é permanente — não retenta. 429/529/rede = transitório.
        for (var tentativa = 1; tentativa <= TentativasMax; tentativa++)
        {
            ct.ThrowIfCancellationRequested();

            HttpResponseMessage? response = null;
            try
            {
                using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
                requestMsg.Content = new StringContent(body, Encoding.UTF8, "application/json");

                response = await client.SendAsync(requestMsg, ct);

                if (response.IsSuccessStatusCode)
                {
                    var respJson = await response.Content.ReadAsStringAsync(ct);
                    return ParsearRespostaBloco(respJson);
                }

                var status = (int)response.StatusCode;

                // 4xx que não seja 429 (TooManyRequests) ou 408 (RequestTimeout): permanente.
                // 401/403 = chave inválida/sem permissão — retentativa não resolveria (CA88/R-R2).
                if (status >= 400 && status < 500
                    && response.StatusCode != HttpStatusCode.TooManyRequests
                    && response.StatusCode != HttpStatusCode.RequestTimeout)
                {
                    _logger.LogError(
                        "[Mapeador] Anthropic API erro permanente {Status} ao classificar bloco '{Hint}'. Nenhuma retentativa.",
                        response.StatusCode, hintNome);
                    throw new InvalidOperationException($"Erro permanente na API de IA: {response.StatusCode}");
                }

                // 429/529/5xx = transitório → retenta.
                // Respeitar Retry-After quando presente (CA87/R-R1).
                var delay = CalcularDelay(response, tentativa);

                _logger.LogWarning(
                    "[Mapeador] Anthropic status {Status} ao classificar bloco '{Hint}' (tentativa {T}/{Max}). Aguardando {DelayMs}ms.",
                    response.StatusCode, hintNome, tentativa, TentativasMax, (int)delay.TotalMilliseconds);

                if (tentativa < TentativasMax)
                    await Task.Delay(delay, ct);
            }
            catch (InvalidOperationException)
            {
                // Erro permanente já categorizado — propagar imediatamente.
                throw;
            }
            catch (HttpRequestException ex)
            {
                // Falha transitória de rede (CA86/R-R1).
                _logger.LogWarning(ex,
                    "[Mapeador] Erro de rede ao classificar bloco '{Hint}' (tentativa {T}/{Max}).",
                    hintNome, tentativa, TentativasMax);

                if (tentativa < TentativasMax)
                {
                    var delay = CalcularBackoffJitter(tentativa);
                    await Task.Delay(delay, ct);
                }
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout da requisição — transitório.
                _logger.LogWarning(
                    "[Mapeador] Timeout ao classificar bloco '{Hint}' (tentativa {T}/{Max}).",
                    hintNome, tentativa, TentativasMax);

                if (tentativa < TentativasMax)
                {
                    var delay = CalcularBackoffJitter(tentativa);
                    await Task.Delay(delay, ct);
                }
            }
            finally
            {
                response?.Dispose();
            }
        }

        // Esgotou todas as tentativas (CA89/R-R1).
        _logger.LogError(
            "[Mapeador] Anthropic: bloco '{Hint}' falhou após {Max} tentativas — será marcado como erro.",
            hintNome, TentativasMax);
        throw new InvalidOperationException($"limite_taxa_ia: bloco '{hintNome}' falhou após {TentativasMax} tentativas");
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

    // ─── Backoff / Retry-After ────────────────────────────────────────────────

    /// <summary>
    /// Calcula delay respeitando Retry-After quando presente; senão usa backoff exponencial + full jitter.
    /// Retry-After: pode ser segundos (int) ou data HTTP (RFC 7231).
    /// </summary>
    private static TimeSpan CalcularDelay(HttpResponseMessage response, int tentativa)
    {
        if (response.Headers.RetryAfter is { } retryAfter)
        {
            // Retry-After: seconds
            if (retryAfter.Delta.HasValue && retryAfter.Delta.Value.TotalSeconds > 0)
                return retryAfter.Delta.Value;

            // Retry-After: HTTP-date
            if (retryAfter.Date.HasValue)
            {
                var espera = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                if (espera > TimeSpan.Zero)
                    return espera;
            }
        }

        return CalcularBackoffJitter(tentativa);
    }

    /// <summary>
    /// Backoff exponencial com full jitter: delay ∈ [0, BaseBackoffMs * 2^(tentativa-1)].
    /// Evita thundering herd em múltiplos jobs simultâneos.
    /// </summary>
    private static TimeSpan CalcularBackoffJitter(int tentativa)
    {
        var maxMs = BaseBackoffMs * Math.Pow(2, tentativa - 1);
        var jitterMs = Random.Shared.NextDouble() * maxMs;
        return TimeSpan.FromMilliseconds(jitterMs);
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
