using System.Text.Json;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Addendum 6 — CA102–CA115.
///
/// Passo de ESCRITA que preenche <c>migracao_registros</c> a partir dos mapas aprovados
/// em <c>migracao_mapas</c>. Roda na transição <c>mapa_em_revisao → preview_pronto</c>,
/// antes de <see cref="PreviewOnda1QueryHandler"/> contar os registros.
///
/// Fluxo por job:
/// 1. Busca o job (falha-fechada: admin, sem filtro de tenant mas job tem EstabelecimentoId).
/// 2. Valida status mapa_em_revisao.
/// 3. Deleta SOMENTE os pendente do job (idempotência — R-M7/CA110).
/// 4. Lista mapas aprovados do job.
/// 5. Para cada mapa aceito (não ignorado, não bloco_com_erro, não sem_equivalente, não eh_config):
///    a. Baixa o arquivo do S3 e parseia (reusa IMigracaoArquivoStorageService + parsers).
///    b. Localiza o bloco pelo NomeBlocoOrigem.
///    c. Para cada linha do bloco, aplica o de-para do mapa e cria MigracaoRegistro.Criar.
/// 6. Salva os registros em lote.
///
/// LGPD: payload_bruto é PII do tenant — nunca logar. Mensagens de erro genéricas.
/// Multi-tenant: EstabelecimentoId herdado do job (R-M1/CA111).
/// Valor real inteiro: nunca truncar a linha (truncamento 500 chars é EXCLUSIVO da IA — CA108/R-M5).
/// Obrigatório NÃO valida aqui — fica na CARGA (R-M6/CA109).
/// </summary>
public sealed class MaterializarRegistrosCommandHandler
{
    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoMapaRepository _mapaRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;
    private readonly IMigracaoArquivoStorageService _storage;
    private readonly IEnumerable<IMigracaoArquivoParser> _parsers;
    private readonly ILogger<MaterializarRegistrosCommandHandler> _logger;

    public MaterializarRegistrosCommandHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoMapaRepository mapaRepo,
        IMigracaoRegistroRepository registroRepo,
        IMigracaoArquivoStorageService storage,
        IEnumerable<IMigracaoArquivoParser> parsers,
        ILogger<MaterializarRegistrosCommandHandler> logger)
    {
        _jobRepo      = jobRepo;
        _mapaRepo     = mapaRepo;
        _registroRepo = registroRepo;
        _storage      = storage;
        _parsers      = parsers;
        _logger       = logger;
    }

    /// <summary>
    /// Materializa (ou re-materializa) os registros pendentes do job.
    /// Chamado no início de <see cref="PreviewOnda1QueryHandler.Handle"/> antes de qualquer contagem.
    /// </summary>
    public async Task ExecutarAsync(long jobId, CancellationToken ct = default)
    {
        if (jobId <= 0) throw new BusinessException("Job inválido.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(jobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        if (job.Status != MigracaoJob.StatusMapaEmRevisao)
            throw new BusinessException("Job precisa estar em revisão de mapa para materializar.");

        // Idempotência (R-M7/CA110): apaga SOMENTE pendentes antes de reger.
        // importado_*/rejeitado/pulado nunca são tocados.
        await _registroRepo.DeletarPendentesPorJob(jobId, ct);

        var mapas = await _mapaRepo.ListarPorJob(jobId, job.EstabelecimentoId, ct);
        if (mapas.Count == 0)
        {
            _logger.LogInformation(
                "[Materializar] Job {JobId}: sem mapas registrados — nenhum registro materializado.", jobId);
            return;
        }

        // Baixa e parseia o arquivo UMA vez, cache em memória por nome de arquivo.
        // Arquivo está limitado a ≤50MB (R12) — parse completo em memória é seguro.
        var blocosPorArquivo = await CarregarBlocosPorArquivoAsync(job.ArquivoS3Key, ct);

        var novos = new List<MigracaoRegistro>();

        foreach (var mapa in mapas)
        {
            if (!DeveMaterializar(mapa))
                continue;

            var dePara = ExtrairDePara(mapa.MapaJson);
            if (dePara is null)
                continue;

            // Localiza o bloco pelo NomeBlocoOrigem dentro dos blocos parseados (D-M3/R-M3).
            var linhas = EncontrarLinhasDoBloco(blocosPorArquivo, mapa.NomeBlocoOrigem);
            if (linhas is null || linhas.Count == 0)
            {
                _logger.LogInformation(
                    "[Materializar] Job {JobId}: bloco '{NomeBloco}' sem linhas — pulado.",
                    jobId, mapa.NomeBlocoOrigem);
                continue;
            }

            var entidade = mapa.Entidade;

            // Valor real inteiro (R-M5/CA108): nunca truncar.
            foreach (var linha in linhas)
            {
                var payload = AplicarDePara(linha, dePara);
                var payloadJson = JsonSerializer.Serialize(payload);
                var registro = MigracaoRegistro.Criar(jobId, job.EstabelecimentoId, entidade, payloadJson);
                novos.Add(registro);
            }
        }

        if (novos.Count > 0)
            await _registroRepo.SalvarLote(novos, ct);

        _logger.LogInformation(
            "[Materializar] Job {JobId}: {Total} registros materializados.", jobId, novos.Count);
    }

    // ── Parsing do arquivo S3 ─────────────────────────────────────────────────

    private async Task<IReadOnlyList<BlocoCandidato>> CarregarBlocosPorArquivoAsync(
        string? arquivoS3Key,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(arquivoS3Key))
            return [];

        // Reusar o mesmo fluxo do InferirMapaMigracaoJobHandler: baixa ZIP, descompacta,
        // parseia cada entrada com o parser adequado.
        await using var zipStream = await _storage.DownloadArquivoAsync(arquivoS3Key, ct);

        using var zip = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);

        var todosBlocos = new List<BlocoCandidato>();

        foreach (var entry in zip.Entries)
        {
            // Ignora arquivos macOS (__MACOSX / ._ prefixo — fix do commit 103eb4f).
            if (entry.FullName.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)) continue;
            if (Path.GetFileName(entry.FullName).StartsWith("._", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var ext = Path.GetExtension(entry.Name);
            var parser = _parsers.FirstOrDefault(p => p.SuportaFormato(ext));
            if (parser is null) continue;

            await using var entryStream = entry.Open();
            // Materializa em MemoryStream para parser (alguns parsers leem duas vezes).
            using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms, ct);
            ms.Position = 0;

            var parseado = await parser.ParsearAsync(ms, entry.Name, ct);
            todosBlocos.AddRange(parseado.Blocos);
        }

        return todosBlocos;
    }

    // ── Predicado: deve este mapa gerar registros? ────────────────────────────

    private static bool DeveMaterializar(MigracaoMapa mapa)
    {
        // R-M4/CA107: sem_equivalente, ignorado, eh_config, bloco_com_erro → não materializa.
        if (string.IsNullOrWhiteSpace(mapa.Entidade)) return false;
        if (mapa.Entidade == "sem_equivalente") return false;

        // Lê flags do MapaJson (gerado pela IA + revisado pelo operador).
        try
        {
            using var doc = JsonDocument.Parse(mapa.MapaJson);
            var root = doc.RootElement;

            // ignorado: true → bloco que operador descartou (CA107).
            if (root.TryGetProperty("ignorado", out var ignoradoProp) && ignoradoProp.GetBoolean())
                return false;

            // eh_config: true → configuração, não migrável (CA107/D-S6).
            if (root.TryGetProperty("eh_config", out var ehConfigProp) && ehConfigProp.GetBoolean())
                return false;

            // bloco_com_erro: true → falhou na inferência (CA115/D-R4).
            if (root.TryGetProperty("bloco_com_erro", out var erroProp) && erroProp.GetBoolean())
                return false;
        }
        catch
        {
            // MapaJson inválido → não materializa (seguro conservador).
            return false;
        }

        return true;
    }

    // ── Extrai de-para do MapaJson ────────────────────────────────────────────

    /// <summary>
    /// Retorna o dicionário col_origem→campo_canônico do MapaJson.
    /// Colunas com valor "ignorar" (case-insensitive) são excluídas (R-M5/CA108).
    /// </summary>
    private static Dictionary<string, string>? ExtrairDePara(string mapaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(mapaJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("de_para", out var dePara) ||
                dePara.ValueKind != JsonValueKind.Object)
                return null;

            var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in dePara.EnumerateObject())
            {
                var campo = prop.Value.GetString() ?? string.Empty;
                // CA108: colunas marcadas "ignorar" são descartadas.
                if (campo.Equals("ignorar", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(campo)) continue;
                resultado[prop.Name] = campo;
            }

            return resultado.Count == 0 ? null : resultado;
        }
        catch
        {
            return null;
        }
    }

    // ── Localiza as linhas de um bloco nos blocos parseados ──────────────────

    private static IReadOnlyList<IReadOnlyDictionary<string, string>>? EncontrarLinhasDoBloco(
        IReadOnlyList<BlocoCandidato> blocos,
        string nomeBlocoOrigem)
    {
        // NomeBlocoOrigem = string.Empty para CSV/JSON-array (arquivo único = 1 bloco).
        // Para dump aninhado, é a chave da propriedade (ex.: "pacientes").
        if (string.IsNullOrEmpty(nomeBlocoOrigem))
        {
            // Primeiro bloco migrável (compatibilidade CSV/JSON-array).
            return blocos.FirstOrDefault(b => !b.EhConfig)?.Linhas;
        }

        return blocos
            .FirstOrDefault(b => b.NomeBloco.Equals(nomeBlocoOrigem, StringComparison.OrdinalIgnoreCase))
            ?.Linhas;
    }

    // ── Aplica o de-para a uma linha (valor real inteiro — R-M5/CA108) ───────

    private static Dictionary<string, string> AplicarDePara(
        IReadOnlyDictionary<string, string> linha,
        Dictionary<string, string> dePara)
    {
        var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (colunaOrigem, campoCanônico) in dePara)
        {
            if (linha.TryGetValue(colunaOrigem, out var valor))
            {
                // Valor real inteiro — NUNCA truncar (CA108/R-M5).
                // O truncamento a 500 chars do addendum 006 é EXCLUSIVO da chamada à IA.
                payload[campoCanônico] = valor;
            }
        }

        return payload;
    }
}
