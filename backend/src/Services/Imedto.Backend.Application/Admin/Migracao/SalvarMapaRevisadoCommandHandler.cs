using System.Text.Json;
using System.Text.Json.Nodes;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Persiste o mapa revisado pelo admin — atualiza de_para, reclassificação e flag ignorado.
/// Addendum 4 (CA77/CA78/CA79): o operador pode:
///   - Corrigir a entidade classificada pela IA (EntidadeReclassificada).
///   - Marcar o bloco como ignorar (Ignorado = true) — não será carregado.
///   - Ajustar o de_para de colunas.
/// Scoped — usa EF via repositório.
/// </summary>
public sealed class SalvarMapaRevisadoCommandHandler
{
    private readonly IMigracaoMapaRepository _mapaRepo;

    public SalvarMapaRevisadoCommandHandler(IMigracaoMapaRepository mapaRepo)
    {
        _mapaRepo = mapaRepo;
    }

    public async Task Handle(SalvarMapaRevisadoCommand cmd, CancellationToken ct = default)
    {
        if (cmd.JobId <= 0) throw new BusinessException("Job inválido.");
        if (string.IsNullOrWhiteSpace(cmd.Entidade)) throw new BusinessException("Entidade inválida.");
        if (cmd.RevisadoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário revisor é obrigatório.");

        // Valida entidade reclassificada (quando presente, deve ser da lista canônica — D-S1/CA77).
        if (!string.IsNullOrWhiteSpace(cmd.EntidadeReclassificada) &&
            !EntidadesCanônicas.EhValida(cmd.EntidadeReclassificada))
        {
            throw new BusinessException("Entidade reclassificada inválida.");
        }

        // Addendum 4: busca por (jobId, entidade, nomeBlocoOrigem) sem filtro de tenant (admin).
        // ObterPorJobEntidadeBlocoAdminOuNulo lida com ambos os casos:
        //   - nomeBlocoOrigem preenchido → dump aninhado, busca pelos 3 campos.
        //   - nomeBlocoOrigem vazio → CSV/JSON-array legado, delega a ObterPorJobEEntidadeAdminOuNulo.
        var mapa = await _mapaRepo.ObterPorJobEntidadeBlocoAdminOuNulo(
            cmd.JobId, cmd.Entidade, cmd.NomeBlocoOrigem ?? string.Empty, ct)
            ?? throw new BusinessException("Mapa não encontrado.");

        // Lê o JSON atual via JsonNode para preservar campos tipados (JsonElement não é IConvertible).
        var jsonAtual = LerMapaJsonNode(mapa.MapaJson);

        // Extrai campos preservados do JSON anterior.
        var entidadeClassificada = jsonAtual?["entidade_classificada"]?.GetValue<string>() ?? cmd.Entidade;
        var confiancaClass        = jsonAtual?["confianca_classificacao"]?.GetValue<double>() ?? 0.0;
        var encodingSuspeito      = jsonAtual?["encoding_suspeito"]?.GetValue<bool>() ?? false;
        var ehConfig              = jsonAtual?["eh_config"]?.GetValue<bool>() ?? false;

        // Determina a entidade final (operador sobrepõe a IA — CA77).
        var entidadeFinal = !string.IsNullOrWhiteSpace(cmd.EntidadeReclassificada)
            ? cmd.EntidadeReclassificada
            : entidadeClassificada;

        // Monta novo JSON preservando classificação e adicionando revisão do operador.
        var novoMapaJson = JsonSerializer.Serialize(new
        {
            de_para                 = cmd.DePara,
            confianca               = 1.0, // revisão manual = confiança máxima no mapa
            duvidas                 = Array.Empty<string>(),
            entidade_classificada   = entidadeClassificada,
            confianca_classificacao = confiancaClass,
            entidade_operador       = entidadeFinal, // sobrescrição pelo operador
            ignorado                = cmd.Ignorado,
            encoding_suspeito       = encodingSuspeito,
            eh_config               = ehConfig,
        });

        mapa.Revisar(novoMapaJson, cmd.RevisadoPorUsuarioId);
        await _mapaRepo.Salvar(mapa, ct);
    }

    private static JsonNode? LerMapaJsonNode(string mapaJson)
    {
        try { return JsonNode.Parse(mapaJson); }
        catch { return null; }
    }
}
