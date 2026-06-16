using System.IO.Compression;
using System.Text.Json;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Infrastructure.Ia;
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
///       - Chama IMapeadorDeMigracao.InferirBlocoAsync — 1 chamada classifica+mapeia (CA74/D-N2).
///       - Entidade detectada é a classificada pela IA (CA73) — não mais pelo nome do arquivo.
///       - Blocos de config (EhConfig=true) são persistidos como "sem_equivalente" para exibição.
///    d. Persiste MigracaoMapa por bloco com entidade_classificada + mapa no JSON.
/// 6. Marca job como mapa_em_revisao.
///
/// Nome: "inferir-mapa-migracao" — registrado em JobsRegistrados (intervalo 30s).
/// LGPD: sem PII em logs — apenas IDs e nomes de bloco.
/// R-S9/D11: id interno do dump nunca é persistido nem usado como chave.
/// </summary>
public sealed class InferirMapaMigracaoJobHandler : IJobHandler
{
    public string Nome => "inferir-mapa-migracao";

    private const int TamanhoAmostra = 10;

    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoArquivoStorageService _storage;
    private readonly IMapeadorDeMigracao _mapeador;
    private readonly IMigracaoMapaRepository _mapaRepo;
    private readonly IMigracaoTemplateRepository _templateRepo;
    private readonly IMigracaoJobEventoRepository _eventoRepo;
    private readonly IEnumerable<IMigracaoArquivoParser> _parsers;
    private readonly ILogger<InferirMapaMigracaoJobHandler> _logger;

    public InferirMapaMigracaoJobHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoArquivoStorageService storage,
        IMapeadorDeMigracao mapeador,
        IMigracaoMapaRepository mapaRepo,
        IMigracaoTemplateRepository templateRepo,
        IMigracaoJobEventoRepository eventoRepo,
        IEnumerable<IMigracaoArquivoParser> parsers,
        ILogger<InferirMapaMigracaoJobHandler> logger)
    {
        _jobRepo      = jobRepo;
        _storage      = storage;
        _mapeador     = mapeador;
        _mapaRepo     = mapaRepo;
        _templateRepo = templateRepo;
        _eventoRepo   = eventoRepo;
        _parsers      = parsers;
        _logger       = logger;
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

        var mapasPersistidos = 0;
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

                _logger.LogInformation(
                    "[Job:{Nome}] Job {JobId} — processando bloco '{Bloco}'.",
                    Nome, job.Id, bloco.NomeBloco);

                await ProcessarBlocoAsync(job, bloco, hintArquivo, templatesPorEntidade, ct);
                mapasPersistidos++;
            }
        }

        // Transição: aguardando_mapa → mapa_em_revisao.
        var statusAnteriorMapa = job.Status;
        job.MarcarMapaEmRevisao();
        await _jobRepo.Salvar(job, ct);

        var evtMapa = MigracaoJobEvento.Criar(job.Id, job.EstabelecimentoId, statusAnteriorMapa, job.Status, usuarioId: null);
        await _eventoRepo.Gravar(evtMapa, ct);

        _logger.LogInformation(
            "[Job:{Nome}] Job {JobId} concluído — {N} mapa(s), status: {Status}.",
            Nome, job.Id, mapasPersistidos, job.Status);
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
                .ToDictionary(kv => kv.Key, kv => PiiSanitizer.Sanitize(kv.Value)))
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
            // Sem template — chama IA (1 chamada por bloco, classifica+mapeia — CA73/CA74/D-N2).
            var esquema = new EsquemaDeArquivo
            {
                Cabecalhos       = bloco.Cabecalhos,
                AmostraMascarada = amostraMascarada,
            };

            // Hint combinado: nome do bloco (ex: "pacientes") + hint do arquivo (ex: "dump_sistema_x").
            var hintFinal = bloco.NomeBloco != hintArquivo
                ? $"{bloco.NomeBloco} (arquivo: {hintArquivo})"
                : hintArquivo;

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

    // ─── helpers ─────────────────────────────────────────────────────────────

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
}
