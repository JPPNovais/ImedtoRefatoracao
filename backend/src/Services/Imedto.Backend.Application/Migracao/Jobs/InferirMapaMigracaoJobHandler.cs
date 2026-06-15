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
/// Fluxo por job:
/// 1. Busca job mais antigo com status aguardando_mapa.
/// 2. Baixa ZIP do S3 via IMigracaoArquivoStorageService.
/// 3. Descompacta em memória (System.IO.Compression.ZipArchive).
/// 4. Se job.Origem tem template cadastrado (CA18/R10), pré-carrega de-para do template.
/// 5. Para cada arquivo CSV/JSON no ZIP:
///    a. Detecta entidade pelo nome do arquivo (ex: pacientes.csv → "paciente").
///    b. Parseia via IMigracaoArquivoParser escolhido por extensão.
///    c. Extrai amostra de até 10 linhas.
///    d. Mascara PII da amostra via PiiSanitizer (CA5).
///    e. Se há template para a entidade, usa de-para do template — não chama IA (CA18).
///       Se não há template, chama IMapeadorDeMigracao.InferirMapaAsync — 1 chamada por arquivo (CA23).
///    f. Persiste MigracaoMapa com a proposta.
/// 6. Marca job como mapa_em_revisao.
///
/// Nome: "inferir-mapa-migracao" — registrado em JobsRegistrados (intervalo 30s).
/// Processa 1 job por rodada para não bloquear o scheduler.
/// LGPD: sem PII em logs — apenas IDs e entidades.
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
    private readonly IEnumerable<IMigracaoArquivoParser> _parsers;
    private readonly ILogger<InferirMapaMigracaoJobHandler> _logger;

    public InferirMapaMigracaoJobHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoArquivoStorageService storage,
        IMapeadorDeMigracao mapeador,
        IMigracaoMapaRepository mapaRepo,
        IMigracaoTemplateRepository templateRepo,
        IEnumerable<IMigracaoArquivoParser> parsers,
        ILogger<InferirMapaMigracaoJobHandler> logger)
    {
        _jobRepo      = jobRepo;
        _storage      = storage;
        _mapeador     = mapeador;
        _mapaRepo     = mapaRepo;
        _templateRepo = templateRepo;
        _parsers      = parsers;
        _logger       = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        // Busca 1 job mais antigo aguardando inferência.
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
            // Addendum 002 — R-B2: mapeia tipo de exceção para categoria legível sem PII.
            // Detalhe técnico só no log estruturado (CA28).
            _logger.LogError(ex,
                "[Job:{Nome}] Falha ao processar inferência do job {JobId}.", Nome, job.Id);

            var motivo = CategorizarFalhaInferencia(ex);
            try
            {
                job.MarcarFalhou(motivo);
                await _jobRepo.Salvar(job, ct);
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

        // CA18/R10 — pré-carrega templates da origem do job (se Origem estiver definida).
        // Templates são buscados por nome da origem e indexados por entidade.
        var templatesPorEntidade = new Dictionary<string, MigracaoTemplate>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(job.Origem))
        {
            var templates = await _templateRepo.ListarPorNome(job.Origem, ct);
            foreach (var t in templates)
                templatesPorEntidade[t.Entidade] = t;

            if (templatesPorEntidade.Count > 0)
                _logger.LogInformation(
                    "[Job:{Nome}] Job {JobId} — {N} template(s) encontrado(s) para origem '{Origem}'.",
                    Nome, job.Id, templatesPorEntidade.Count, job.Origem);
        }

        // Baixa ZIP do S3.
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

            var entidade = DetectarEntidade(entry.Name);
            _logger.LogInformation(
                "[Job:{Nome}] Job {JobId} — processando entidade '{Entidade}'.", Nome, job.Id, entidade);

            await using var entryStream = entry.Open();
            var parseado = await parser.ParsearAsync(entryStream, entry.Name, ct);

            string mapaJson;

            // CA18 — se há template para esta entidade, usa o de-para do template (não chama IA).
            // O operador ainda pode editar na revisão — o template é ponto de partida, não definitivo.
            if (templatesPorEntidade.TryGetValue(entidade, out var templateDaEntidade))
            {
                _logger.LogInformation(
                    "[Job:{Nome}] Job {JobId} — entidade '{Entidade}' pré-preenchida pelo template.",
                    Nome, job.Id, entidade);

                // Reutiliza o MapaJson do template diretamente — inclui de_para, confiança e dúvidas.
                mapaJson = templateDaEntidade.MapaJson;
            }
            else
            {
                // Sem template — chama IA (1 chamada por arquivo — CA23).
                var amostraBruta = parseado.Linhas.Take(TamanhoAmostra).ToList();

                // Mascara PII em cada valor da amostra (CA5).
                var amostraMascarada = amostraBruta
                    .Select(linha => (IReadOnlyDictionary<string, string>)linha
                        .ToDictionary(kv => kv.Key, kv => PiiSanitizer.Sanitize(kv.Value)))
                    .ToList();

                var esquema = new EsquemaDeArquivo
                {
                    Cabecalhos       = parseado.Cabecalhos,
                    AmostraMascarada = amostraMascarada,
                };

                // 1 chamada à IA por arquivo (CA23).
                var proposta = await _mapeador.InferirMapaAsync(esquema, entidade, ct);

                mapaJson = JsonSerializer.Serialize(new
                {
                    de_para   = proposta.DeParaColunas,
                    confianca = proposta.Confianca,
                    duvidas   = proposta.Duvidas,
                });
            }

            // Upsert: atualiza se já existir mapa para essa entidade.
            var mapaExistente = await _mapaRepo.ObterPorJobEEntidadeOuNulo(
                job.Id, entidade, job.EstabelecimentoId, ct);

            if (mapaExistente is not null)
            {
                // Guid.Empty = atualização automática pelo job (não por usuário humano).
                mapaExistente.Revisar(mapaJson, Guid.Empty);
                await _mapaRepo.Salvar(mapaExistente, ct);
            }
            else
            {
                var novoMapa = MigracaoMapa.Criar(job.Id, job.EstabelecimentoId, entidade, mapaJson);
                await _mapaRepo.Salvar(novoMapa, ct);
            }

            mapasPersistidos++;
        }

        // Transição de estado: aguardando_mapa → mapa_em_revisao.
        job.MarcarMapaEmRevisao();
        await _jobRepo.Salvar(job, ct);

        _logger.LogInformation(
            "[Job:{Nome}] Job {JobId} concluído — {N} mapa(s) gerado(s), status: {Status}.",
            Nome, job.Id, mapasPersistidos, job.Status);
    }

    /// <summary>
    /// Detecta entidade pelo nome do arquivo.
    /// Ex.: "pacientes.csv" → "paciente", "agendamentos.json" → "agendamento".
    /// </summary>
    private static string DetectarEntidade(string nomeArquivo)
    {
        var nomeSemExtensao = Path.GetFileNameWithoutExtension(nomeArquivo)
            .ToLowerInvariant()
            .Trim();

        // Remove sufixo plural comum em português.
        return nomeSemExtensao.TrimEnd('s');
    }

    /// <summary>
    /// Addendum 002 — R-B2: mapeia tipo de exceção para categoria legível PT-BR, sem PII.
    /// A mensagem técnica fica apenas no log estruturado.
    /// </summary>
    private static string CategorizarFalhaInferencia(Exception ex)
    {
        // Chave/credencial de IA ausente ou inválida.
        if (ex is InvalidOperationException && ex.Message.Contains("ApiKey", StringComparison.OrdinalIgnoreCase))
            return "IA não configurada";
        if (ex is System.Net.Http.HttpRequestException httpEx && (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized || httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden))
            return "IA não configurada";

        // Falha ao baixar o arquivo do S3.
        if (ex.Message.Contains("S3", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("download", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase))
            return "falha ao baixar o arquivo";

        // Arquivo corrompido ou ilegível (descompactação/parse).
        if (ex is InvalidDataException || ex is System.IO.IOException)
            return "arquivo corrompido ou ilegível";

        // Falha genérica na inferência de mapa.
        return "falha ao gerar o mapa";
    }
}
