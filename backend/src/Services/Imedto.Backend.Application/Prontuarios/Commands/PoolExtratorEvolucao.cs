using System.Text.Json;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Extrai nomes de itens do ConteudoJson de uma evolução e cria, transacionalmente,
/// os que ainda não existem no pool do estabelecimento.
///
/// Mapeamento (briefing 2026-06-05_001 + addendum):
///   hpp.alergias[].nome      → Alergia
///   hpp.medicacoes[].nome    → Medicamento
///   hpp.cirurgias[].nome     → Cirurgia
///   hpp.doencas[].nome       → Doenca
///   h-familiar.parentes[].parentesco → RelacaoFamiliar
///
/// Expectativa: fora desta entrega (sem campo no ConteudoJson).
/// Falha-suave: chave ausente ou JSON inesperado não interrompe o salvamento da evolução.
/// LGPD: apenas o campo `nome`/`parentesco` vira item do pool. Campos livres jamais.
/// </summary>
public class PoolExtratorEvolucao
{
    private readonly IProntuarioVariavelPoolRepository _poolRepo;

    public PoolExtratorEvolucao(IProntuarioVariavelPoolRepository poolRepo)
    {
        _poolRepo = poolRepo;
    }

    public async Task ExtrairECriar(long estabelecimentoId, string conteudoJson)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(conteudoJson);
        }
        catch
        {
            // JSON inválido → falha-suave; não quebra o salvamento da evolução.
            return;
        }

        using (doc)
        {
            await ExtrairSecaoHpp(estabelecimentoId, doc.RootElement);
            await ExtrairHistoriaFamiliar(estabelecimentoId, doc.RootElement);
        }
    }

    // ── HPP ────────────────────────────────────────────────────────────────────

    private async Task ExtrairSecaoHpp(long estabelecimentoId, JsonElement root)
    {
        if (!root.TryGetProperty("hpp", out var hpp))
            return;

        await ExtrairNomesDeArray(estabelecimentoId, hpp, "alergias", "nome", TipoVariavelPool.Alergia);
        await ExtrairNomesDeArray(estabelecimentoId, hpp, "medicacoes", "nome", TipoVariavelPool.Medicamento);
        await ExtrairNomesDeArray(estabelecimentoId, hpp, "cirurgias", "nome", TipoVariavelPool.Cirurgia);
        await ExtrairNomesDeArray(estabelecimentoId, hpp, "doencas", "nome", TipoVariavelPool.Doenca);
    }

    // ── História familiar ───────────────────────────────────────────────────────

    private async Task ExtrairHistoriaFamiliar(long estabelecimentoId, JsonElement root)
    {
        // O JSON usa a chave "h-familiar" conforme SecaoHistoriaFamiliar.vue.
        if (!root.TryGetProperty("h-familiar", out var hf))
            return;

        await ExtrairNomesDeArray(estabelecimentoId, hf, "parentes", "parentesco", TipoVariavelPool.RelacaoFamiliar);
    }

    // ── Helpers internos ────────────────────────────────────────────────────────

    private async Task ExtrairNomesDeArray(
        long estabelecimentoId,
        JsonElement secao,
        string arrayKey,
        string campoNome,
        TipoVariavelPool tipo)
    {
        if (!secao.TryGetProperty(arrayKey, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return;

        // Carrega lista existente uma vez para dedup em memória (evita N+1 no banco).
        var existentes = await _poolRepo.ListarAtivosPorTipo(estabelecimentoId, tipo);
        var nomesNormExistentes = existentes
            .Select(e => NormalizadorPool.Normalizar(e.Nome))
            .ToHashSet();

        foreach (var item in arr.EnumerateArray())
        {
            if (!item.TryGetProperty(campoNome, out var nomeProp))
                continue;

            var nome = nomeProp.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(nome))
                continue; // CA9: ignora vazios

            var nomeNorm = NormalizadorPool.Normalizar(nome);

            // CA3/CA4: reusa padrão-sistema sem criar cópia; não duplica.
            if (nomesNormExistentes.Contains(nomeNorm))
                continue;

            var novoItem = ProntuarioVariavelPool.CriarDoEstabelecimento(estabelecimentoId, tipo, nome);
            await _poolRepo.Salvar(novoItem);

            // Registra localmente para evitar duplicatas dentro do mesmo array
            // (ex.: duas alergias com o mesmo nome na mesma evolução).
            nomesNormExistentes.Add(nomeNorm);
        }
    }
}
