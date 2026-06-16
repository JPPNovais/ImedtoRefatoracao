using System.IO.Compression;
using System.Text.Json;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Infrastructure.Ia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.Migracao.Jobs;

/// <summary>
/// Job recorrente que processa 1 job de migração em estado "aguardando_mapa" por rodada.
///
/// Fluxo por job (addendum 4 — classificação semântica + dump JSON aninhado):
/// 1. Busca job mais antigo com status aguardando_mapa.
/// 2. Baixa ZIP do S3 via IMigracaoArquivoStorageService.
/// 3. Descompacta em memória.
/// 4. Se job.Origem tem template, pré-carrega de-para (CA18/R10).
/// 5. Para cada arquivo CSV/JSON no ZIP:
///    a. Parseia via IMigracaoArquivoParser → obtém Blocos (N blocos por JSON dump, 1 por CSV).
///    b. Normaliza encoding (MojibakeNormalizador — já aplicado no parser).
///    c. Para cada bloco-candidato migrável (não EhConfig):
///       - Extrai amostra de até 10 linhas mascaradas.
///       - Trunca cada valor a 500 chars após a máscara de PII (CA91/R-R4/D-R3).
///       - Pula chamada de IA se bloco já tem mapa bem-sucedido (CA97/R-R8/D-R5).
///       - Chama IMapeadorDeMigracao.InferirBlocoAsync — 1 chamada classifica+mapeia (CA74/D-N2).
///       - Aguarda pausa fixa entre blocos para não saturar o TPM (CA90/R-R3/D-R2).
///       - Entidade detectada é a classificada pela IA (CA73) — não mais pelo nome do arquivo.
///       - Blocos de config (EhConfig=true) são persistidos como "sem_equivalente" para exibição.
///    d. Persiste MigracaoMapa por bloco com entidade_classificada + mapa no JSON.
/// 6. Degradação graciosa (CA92-CA96/D-R4):
///    - Falha de IA em um bloco NÃO derruba o job.
///    - ≥1 bloco com sucesso → mapa_em_revisao com aviso.
///    - Zero sucesso → falhou.
///
/// Nome: "inferir-mapa-migracao" — registrado em JobsRegistrados (intervalo 30s).
/// LGPD: sem PII em logs — apenas IDs e nomes de bloco.
/// R-S9/D11: id interno do dump nunca é persistido nem usado como chave.
/// </summary>
public sealed class InferirMapaMigracaoJobHandler : IJobHandler
{
    public string Nome => "inferir-mapa-migracao";

    private const int TamanhoAmostra = 10;

    // Truncamento de valor na amostra (CA91/R-R4/D-R3) — após máscara de PII, antes da IA.
    private const int MaxCharsValorAmostra = 500;
    private const string MarcadorTruncado = "…[truncado]";

    // Espaçamento fixo entre blocos (CA90/R-R3/D-R2). Configurável via Ia:PausaEntreBlocosMs.
    private const int PausaEntreBlocosDefaultMs = 1000;

    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoArquivoStorageService _storage;
    private readonly IMapeadorDeMigracao _mapeador;
    private readonly IMigracaoMapaRepository _mapaRepo;
    private readonly IMigracaoTemplateRepository _templateRepo;
    private readonly IMigracaoJobEventoRepository _eventoRepo;
    private readonly IEnumerable<IMigracaoArquivoParser> _parsers;
    private readonly ILogger<InferirMapaMigracaoJobHandler> _logger;
    private readonly int _pausaEntreBlocosMs;

    public InferirMapaMigracaoJobHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoArquivoStorageService storage,
        IMapeadorDeMigracao mapeador,
        IMigracaoMapaRepository mapaRepo,
        IMigracaoTemplateRepository templateRepo,
        IMigracaoJobEventoRepository eventoRepo,
        IEnumerable<IMigracaoArquivoParser> parsers,
        ILogger<InferirMapaMigracaoJobHandler> logger,
        IConfiguration? config = null)
    {
        _jobRepo      = jobRepo;
        _storage      = storage;
        _mapeador     = mapeador;
        _mapaRepo     = mapaRepo;
        _templateRepo = templateRepo;
        _eventoRepo   = eventoRepo;
        _parsers      = parsers;
        _logger       = logger;
        _pausaEntreBlocosMs = int.TryParse(
            config?["Ia:PausaEntreBlocosMs"], out var v) ? v : PausaEntreBlocosDefaultMs;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var job = await _jobRepo.ObterMaisAntigoAguardandoMapaOuNulo(ct);
        if (job is null)
        {
            _logger.LogDebug("[Job:{Nome}] Nenhum job aguardando inferência de mapa.", Nome);
            return;
        }

        _logger.LogInformation(
            "[Job:{Nome}] Iniciando inferência de mapa para job {JobId}.", Nome, job.Id);

        try
        {
            await ProcessarJobAsync(job, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Job:{Nome}] Falha ao processar inferência do job {JobId}.", Nome, job.Id);

            var motivo = CategorizarFalhaInferencia(ex);
            var statusAnteriorFalha = job.Status;
            try
            {
                job.MarcarFalhou(motivo);
                await _jobRepo.Salvar(job, ct);

                var evt = MigracaoJobEvento.Criar(job.Id, job.EstabelecimentoId, statusAnteriorFalha, job.Status, usuarioId: null);
                await _eventoRepo.Gravar(evt, ct);
            }
            catch (Exception salvarEx)
            {
                _logger.LogError(salvarEx,
                    "[Job:{Nome}] Falha ao persistir status 'falhou' do job {JobId}.", Nome, job.Id);
            }
        }
    }

    private async Task ProcessarJobAsync(MigracaoJob job, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(job.ArquivoS3Key))
        {
            _logger.LogWarning(
                "[Job:{Nome}] Job {JobId} não tem ArquivoS3Key — ignorando.", Nome, job.Id);
            return;
        }

        // CA18/R10 — pré-carrega templates da origem do job.
        var templatesPorEntidade = new Dictionary<string, MigracaoTemplate>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(job.Origem))
        {
            var templates = await _templateRepo.ListarPorNome(job.Origem, ct);
            foreach (var t in templates)
                templatesPorEntidade[t.Entidade] = t;

            if (templatesPorEntidade.Count > 0)
                _logger.LogInformation(
                    "[Job:{Nome}] Job {JobId} — {N} template(s) para origem '{Origem}'.",
                    Nome, job.Id, templatesPorEntidade.Count, job.Origem);
        }

        await using var zipStream = await _storage.DownloadArquivoAsync(job.ArquivoS3Key, ct);
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        // Degradação por bloco (CA92-CA96/D-R4):
        // Rastreia quantos blocos mapearam e quais falharam.
        var blocosSucesso = 0;
        var blocosComErro = new List<string>(); // nomes dos blocos que falharam após esgotar retry
        var primeiroBloco = true;

        foreach (var entry in zipArchive.Entries)
        {
            if (ct.IsCancellationRequested) break;

            var extensao = Path.GetExtension(entry.Name).ToLowerInvariant();
            var parser = _parsers.FirstOrDefault(p => p.SuportaFormato(extensao));
            if (parser is null)
            {
                _logger.LogDebug(
                    "[Job:{Nome}] Job {JobId} — arquivo {Ext} sem parser suportado — ignorado.",
                    Nome, job.Id, extensao);
                continue;
            }

            // Hint do arquivo (nome sem extensão) — passado à IA como contexto, não como decisão (R-S3).
            var hintArquivo = Path.GetFileNameWithoutExtension(entry.Name);

            await using var entryStream = entry.Open();
            var parseado = await parser.ParsearAsync(entryStream, entry.Name, ct);

            // Addendum 4: itera sobre todos os blocos-candidatos do arquivo (CA70/R-S1).
            // Para CSV/JSON-array: 1 bloco. Para dump JSON aninhado: N blocos.
            foreach (var bloco in parseado.Blocos)
            {
                if (ct.IsCancellationRequested) break;

                // Blocos de config (ex.: estabelecimento{}) são persistidos como "sem_equivalente"
                // para exibição no painel — não são carregados (D-S6/CA78/R-S7).
                if (bloco.EhConfig)
                {
                    await PersistirBlocoConfig(job, bloco, ct);
                    continue;
                }

                // Espaçamento fixo entre blocos (CA90/R-R3/D-R2) — nunca antes do primeiro.
                if (!primeiroBloco && _pausaEntreBlocosMs > 0)
                    await Task.Delay(_pausaEntreBlocosMs, ct);

                primeiroBloco = false;

                _logger.LogInformation(
                    "[Job:{Nome}] Job {JobId} — processando bloco '{Bloco}'.",
                    Nome, job.Id, bloco.NomeBloco);

                // Degradação graciosa: falha de bloco não derruba o job (CA92/R-R5/D-R4).
                try
                {
                    await ProcessarBlocoAsync(job, bloco, hintArquivo, templatesPorEntidade, ct);
                    blocosSucesso++;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    // Bloco virou mapa de erro — persiste e continua (CA93/R-R5).
                    blocosComErro.Add(bloco.NomeBloco);
                    var motivoErro = CategorizarFalhaBloco(ex);
                    _logger.LogWarning(
                        "[Job:{Nome}] Job {JobId} — bloco '{Bloco}' falhou ({Motivo}). Continuando com os demais.",
                        Nome, job.Id, bloco.NomeBloco, motivoErro);

                    await PersistirBlocoErro(job, bloco, motivoErro, ct);
                }
            }
        }

        // Ao fim: decide transição do job (CA95/CA94/R-R6/D-R4).
        if (blocosSucesso == 0 && blocosComErro.Count > 0)
        {
            // Zero sucesso → falhou (CA95).
            _logger.LogWarning(
                "[Job:{Nome}] Job {JobId} — zero blocos com sucesso. Marcando como falhou.",
                Nome, job.Id);

            var motivo = "falha na inferência — nenhum bloco pôde ser classificado";
            var statusAnteriorZero = job.Status;
            job.MarcarFalhou(motivo);
            await _jobRepo.Salvar(job, ct);

            var evtZero = MigracaoJobEvento.Criar(
                job.Id, job.EstabelecimentoId, statusAnteriorZero, job.Status, usuarioId: null);
            await _eventoRepo.Gravar(evtZero, ct);
            return;
        }

        // ≥1 sucesso → mapa_em_revisao com aviso de blocos falhos (CA94/R-R6/R-R7).
        var statusAnteriorMapa = job.Status;
        job.MarcarMapaEmRevisao();
        await _jobRepo.Salvar(job, ct);

        var evtMapa = MigracaoJobEvento.Criar(
            job.Id, job.EstabelecimentoId, statusAnteriorMapa, job.Status, usuarioId: null);
        await _eventoRepo.Gravar(evtMapa, ct);

        if (blocosComErro.Count > 0)
        {
            _logger.LogWarning(
                "[Job:{Nome}] Job {JobId} concluído com {NErr} bloco(s) com erro: {Blocos}.",
                Nome, job.Id, blocosComErro.Count, string.Join(", ", blocosComErro));
        }

        _logger.LogInformation(
            "[Job:{Nome}] Job {JobId} concluído — {NOk} bloco(s) OK, {NErr} com erro, status: {Status}.",
            Nome, job.Id, blocosSucesso, blocosComErro.Count, job.Status);
    }

    // ─── Processamento de bloco migrável ─────────────────────────────────────

    private async Task ProcessarBlocoAsync(
        MigracaoJob job,
        BlocoCandidato bloco,
        string hintArquivo,
        Dictionary<string, MigracaoTemplate> templatesPorEntidade,
        CancellationToken ct)
    {
        var encodingSuspeito = bloco.EncodingSuspeito;

        string mapaJson;
        string entidadeParaPersistir;

        // Amostra mascarada (R-S4/CA75/D2).
        var amostraBruta = bloco.Linhas.Take(TamanhoAmostra).ToList();
        var amostraMascarada = amostraBruta
            .Select(linha => (IReadOnlyDictionary<string, string>)linha
                .ToDictionary(
                    kv => kv.Key,
                    kv => TruncarValor(PiiSanitizer.Sanitize(kv.Value)))) // CA91: truncar APÓS máscara
            .ToList();

        // CA18 — template pré-carregado por entidade?
        // Para templates, usamos o nome do bloco como hint de entidade, mas a classificação é do template.
        // Compatibilidade: template indexado por entidade (nome do bloco sem 's' — legado tabular).
        var entidadeHint = bloco.NomeBloco.TrimEnd('s');
        if (templatesPorEntidade.TryGetValue(entidadeHint, out var templateDaEntidade))
        {
            _logger.LogInformation(
                "[Job:{Nome}] Job {JobId} — bloco '{Bloco}' pré-preenchido pelo template.",
                Nome, job.Id, bloco.NomeBloco);
            entidadeParaPersistir = entidadeHint;
            mapaJson = InjetarMetadadosNoMapaJson(
                templateDaEntidade.MapaJson,
                entidadeClassificada: entidadeHint,
                confiancaClassificacao: 1.0,
                ignorado: false,
                encodingSuspeito: encodingSuspeito);
        }
        else
        {
            // Reprocessar parcial (CA97/R-R8/D-R5): se bloco já tem mapa bem-sucedido, pula a IA.
            // Usa ObterPorJobBlocoOuNulo para buscar independente da entidade persistida.
            var mapaExistenteParcial = await _mapaRepo.ObterPorJobBlocoOuNulo(
                job.Id, bloco.NomeBloco, job.EstabelecimentoId, ct);

            if (mapaExistenteParcial is not null && !BlocoTemErro(mapaExistenteParcial.MapaJson))
            {
                // Bloco já mapeado com sucesso — não rechamar a IA (CA97/R-R8).
                _logger.LogInformation(
                    "[Job:{Nome}] Job {JobId} — bloco '{Bloco}' já mapeado com sucesso — pulando IA.",
                    Nome, job.Id, bloco.NomeBloco);
                return;
            }

            // Sem template e sem mapa OK — chama IA (1 chamada por bloco, classifica+mapeia — CA73/CA74/D-N2).
            var esquema = new EsquemaDeArquivo
            {
                Cabecalhos       = bloco.Cabecalhos,
                AmostraMascarada = amostraMascarada,
            };

            // Hint combinado: nome do bloco (ex: "pacientes") + hint do arquivo (ex: "dump_sistema_x").
            var hintFinal = bloco.NomeBloco != hintArquivo
                ? $"{bloco.NomeBloco} (arquivo: {hintArquivo})"
                : hintArquivo;

            // Retry e backoff vivem no adapter — handler só vê sucesso ou exceção (CA86-CA89/D-R1).
            var proposta = await _mapeador.InferirBlocoAsync(esquema, hintFinal, ct);

            // Entidade classificada pela IA (CA73) — não pelo nome do arquivo.
            entidadeParaPersistir = proposta.EntidadeClassificada;

            mapaJson = JsonSerializer.Serialize(new
            {
                de_para                 = proposta.DeParaColunas,
                confianca               = proposta.Confianca,
                duvidas                 = proposta.Duvidas,
                entidade_classificada   = proposta.EntidadeClassificada,
                confianca_classificacao = proposta.ConfiancaClassificacao,
                ignorado                = entidadeParaPersistir == EntidadesCanônicas.SemEquivalente,
                encoding_suspeito       = encodingSuspeito,
            });
        }

        // Upsert por (jobId, entidade, nomeBlocoOrigem) — resolve colisão (addendum 4, §9).
        var mapaExistente = await _mapaRepo.ObterPorJobEntidadeBlocoOuNulo(
            job.Id, entidadeParaPersistir, bloco.NomeBloco, job.EstabelecimentoId, ct);

        if (mapaExistente is not null)
        {
            mapaExistente.Revisar(mapaJson, Guid.Empty);
            await _mapaRepo.Salvar(mapaExistente, ct);
        }
        else
        {
            var novoMapa = MigracaoMapa.Criar(
                job.Id, job.EstabelecimentoId, entidadeParaPersistir, mapaJson, bloco.NomeBloco);
            await _mapaRepo.Salvar(novoMapa, ct);
        }
    }

    // ─── Blocos de config (ex.: estabelecimento{}) ───────────────────────────

    /// <summary>
    /// Persiste bloco de config como "sem_equivalente" para exibição no painel (D-S6/CA78).
    /// O operador vê e decide — default = ignorar.
    /// </summary>
    private async Task PersistirBlocoConfig(MigracaoJob job, BlocoCandidato bloco, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Job:{Nome}] Job {JobId} — bloco '{Bloco}' é config (não migrável) — sinalizado.",
            Nome, job.Id, bloco.NomeBloco);

        var mapaJson = JsonSerializer.Serialize(new
        {
            de_para                 = new Dictionary<string, string>(),
            confianca               = 0.0,
            duvidas                 = Array.Empty<string>(),
            entidade_classificada   = EntidadesCanônicas.SemEquivalente,
            confianca_classificacao = 0.0,
            ignorado                = true,
            encoding_suspeito       = false,
            eh_config               = true,
        });

        var mapaExistente = await _mapaRepo.ObterPorJobEntidadeBlocoOuNulo(
            job.Id, EntidadesCanônicas.SemEquivalente, bloco.NomeBloco, job.EstabelecimentoId, ct);

        if (mapaExistente is not null)
        {
            mapaExistente.Revisar(mapaJson, Guid.Empty);
            await _mapaRepo.Salvar(mapaExistente, ct);
        }
        else
        {
            var novoMapa = MigracaoMapa.Criar(
                job.Id, job.EstabelecimentoId, EntidadesCanônicas.SemEquivalente, mapaJson, bloco.NomeBloco);
            await _mapaRepo.Salvar(novoMapa, ct);
        }
    }

    // ─── Bloco de erro de IA (CA92/CA93/R-R5/D-R4) ───────────────────────────

    /// <summary>
    /// Persiste bloco com falha de IA como mapa de erro:
    /// bloco_com_erro=true + motivo genérico sem PII no mapa_json.
    /// </summary>
    private async Task PersistirBlocoErro(
        MigracaoJob job,
        BlocoCandidato bloco,
        string motivoErro,
        CancellationToken ct)
    {
        var mapaJson = JsonSerializer.Serialize(new
        {
            de_para                 = new Dictionary<string, string>(),
            confianca               = 0.0,
            duvidas                 = Array.Empty<string>(),
            entidade_classificada   = EntidadesCanônicas.SemEquivalente,
            confianca_classificacao = 0.0,
            ignorado                = false,
            encoding_suspeito       = bloco.EncodingSuspeito,
            bloco_com_erro          = true,
            motivo_erro             = motivoErro, // Categoria genérica sem PII (CA99/R-R7)
        });

        // Upsert: se há mapa de erro anterior do mesmo bloco, sobrescreve.
        var mapaExistente = await _mapaRepo.ObterPorJobEntidadeBlocoOuNulo(
            job.Id, EntidadesCanônicas.SemEquivalente, bloco.NomeBloco, job.EstabelecimentoId, ct);

        if (mapaExistente is not null)
        {
            mapaExistente.Revisar(mapaJson, Guid.Empty);
            await _mapaRepo.Salvar(mapaExistente, ct);
        }
        else
        {
            var novoMapa = MigracaoMapa.Criar(
                job.Id, job.EstabelecimentoId, EntidadesCanônicas.SemEquivalente, mapaJson, bloco.NomeBloco);
            await _mapaRepo.Salvar(novoMapa, ct);
        }
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Trunca valor de campo da amostra a 500 chars após a máscara de PII (CA91/R-R4/D-R3).
    /// Mantém D1/D2: a máscara roda antes; truncamento só limita comprimento.
    /// </summary>
    private static string TruncarValor(string valor)
    {
        if (valor.Length <= MaxCharsValorAmostra) return valor;
        return string.Concat(valor.AsSpan(0, MaxCharsValorAmostra), MarcadorTruncado);
    }

    /// <summary>
    /// Verifica se um mapa_json representa um bloco com erro de IA.
    /// Usado no reprocessar parcial (CA97/R-R8): bloco OK → pular chamada de IA.
    /// </summary>
    private static bool BlocoTemErro(string mapaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(mapaJson);
            if (doc.RootElement.TryGetProperty("bloco_com_erro", out var prop))
                return prop.ValueKind == JsonValueKind.True;
            return false;
        }
        catch
        {
            // Mapa inválido → trata como erro para forçar re-inferência.
            return true;
        }
    }

    /// <summary>
    /// Injeta campos de classificação do addendum 4 no mapaJson existente (de template).
    /// Preserva os campos originais do template e adiciona os novos sem sobrescrever.
    /// </summary>
    private static string InjetarMetadadosNoMapaJson(
        string mapaJsonOriginal,
        string entidadeClassificada,
        double confiancaClassificacao,
        bool ignorado,
        bool encodingSuspeito)
    {
        try
        {
            using var doc = JsonDocument.Parse(mapaJsonOriginal);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(mapaJsonOriginal)
                       ?? new Dictionary<string, JsonElement>();

            // Recria como dict object para adicionar campos.
            var obj = new Dictionary<string, object?>();
            foreach (var (k, v) in dict)
            {
                obj[k] = v.ValueKind == JsonValueKind.String ? v.GetString()
                    : v.ValueKind == JsonValueKind.Number ? (object)v.GetDouble()
                    : (object)v.GetRawText();
            }
            obj["entidade_classificada"]   = entidadeClassificada;
            obj["confianca_classificacao"] = confiancaClassificacao;
            obj["ignorado"]                = ignorado;
            obj["encoding_suspeito"]       = encodingSuspeito;

            return JsonSerializer.Serialize(obj);
        }
        catch
        {
            // Fallback: não corrompe o mapaJson original.
            return mapaJsonOriginal;
        }
    }

    /// <summary>
    /// Addendum 002 — R-B2: mapeia tipo de exceção para categoria legível PT-BR, sem PII.
    /// Usado quando TODA a inferência (download/ZIP/parsers) falha — job vai para "falhou".
    /// </summary>
    private static string CategorizarFalhaInferencia(Exception ex)
    {
        if (ex is InvalidOperationException && ex.Message.Contains("ApiKey", StringComparison.OrdinalIgnoreCase))
            return "IA não configurada";
        if (ex is System.Net.Http.HttpRequestException httpEx &&
            (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
             httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden))
            return "IA não configurada";

        if (ex.Message.Contains("S3", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("download", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase))
            return "falha ao baixar o arquivo";

        if (ex is InvalidDataException || ex is System.IO.IOException)
            return "arquivo corrompido ou ilegível";

        return "falha ao gerar o mapa";
    }

    /// <summary>
    /// Categoriza falha de IA em um único bloco (CA92/R-R5/CA99 — categoria genérica sem PII).
    /// Motivos fixos: limite_taxa_ia | provider_indisponivel | falha_classificacao.
    /// </summary>
    private static string CategorizarFalhaBloco(Exception ex)
    {
        var msg = ex.Message;

        if (msg.Contains("limite_taxa_ia", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("429", StringComparison.OrdinalIgnoreCase))
            return "limite_taxa_ia";

        if (ex is System.Net.Http.HttpRequestException ||
            msg.Contains("overload", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("provider", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("indisponível", StringComparison.OrdinalIgnoreCase))
            return "provider_indisponivel";

        return "falha_classificacao";
    }
}
