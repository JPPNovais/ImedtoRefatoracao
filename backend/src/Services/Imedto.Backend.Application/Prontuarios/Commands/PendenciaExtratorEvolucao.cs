using System.Text.Json;
using Imedto.Backend.Domain.Prontuarios.Pendencias;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Extrai as ações de conduta marcadas no ConteudoJson da evolução e cria pendências
/// de atendimento de forma idempotente e falha-suave (mesmo padrão do PoolExtratorEvolucao).
///
/// Falha-suave (CA75/R2): qualquer exceção é capturada — nunca interrompe o salvamento.
/// Idempotência (CA62/R3): verifica existência antes de criar; constraint UNIQUE no banco é trava dura.
///
/// Formato esperado do ConteudoJson no nó "conduta":
///   { "acoesMarcadas": ["CriarReceita", "AgendarRetorno"], "observacao": "..." }
///
/// LGPD (R4/CA71): apenas os nomes das ações (enum) são extraídos. "observacao" fica no ConteudoJson e
/// nunca entra na pendência.
/// </summary>
public class PendenciaExtratorEvolucao
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public PendenciaExtratorEvolucao(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task ExtrairECriar(
        long estabelecimentoId,
        long pacienteId,
        long evolucaoId,
        long? agendamentoId,
        Guid autorUsuarioId,
        string conteudoJson)
    {
        try
        {
            var acoes = ParsearAcoesMarcadas(conteudoJson);
            if (acoes.Count == 0)
                return;

            foreach (var acao in acoes)
            {
                // MarcarProcedimentoRealizado só vira pendência se a evolução tiver ao menos
                // 1 procedimento indicado válido (espelha a condição do command handler, que
                // lança 422 sem procedimentos/valor). Sem isso, marcar o checkbox de conduta sem
                // preencher "procedimentos-indicados" criaria uma pendência impossível de concluir.
                if (acao == AcaoPendencia.MarcarProcedimentoRealizado &&
                    !TemProcedimentosIndicadosValidos(conteudoJson))
                    continue;

                // Idempotência: se já existe pendência para este par (evolucao, acao), não cria.
                var jaExiste = await _repo.ExistePorEvolucaoEAcao(evolucaoId, acao);
                if (jaExiste)
                    continue;

                var pendencia = PendenciaAtendimento.Criar(
                    estabelecimentoId,
                    pacienteId,
                    evolucaoId,
                    agendamentoId,
                    acao,
                    autorUsuarioId);

                await _repo.Salvar(pendencia);
            }
        }
        catch
        {
            // Falha-suave: erro ao criar pendências nunca derruba a evolução (CA75/R2).
        }
    }

    /// <summary>
    /// Verifica se o ConteudoJson tem ao menos 1 procedimento indicado válido (com catalogoCirurgiaId)
    /// e valor total > 0 — mesma condição que MarcarProcedimentoRealizadoCommandHandler exige para gerar
    /// cobrança. Mantém o par criação↔conclusão consistente: não cria pendência que o comando recusaria.
    /// </summary>
    private static bool TemProcedimentosIndicadosValidos(string conteudoJson)
    {
        if (string.IsNullOrWhiteSpace(conteudoJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(conteudoJson);
            if (!doc.RootElement.TryGetProperty("procedimentos-indicados", out var secao))
                return false;
            if (!secao.TryGetProperty("procedimentos", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return false;

            decimal total = 0m;
            var temItem = false;
            foreach (var item in arr.EnumerateArray())
            {
                if (!item.TryGetProperty("catalogoCirurgiaId", out var idEl) || idEl.ValueKind == JsonValueKind.Null)
                    continue;
                temItem = true;
                if (item.TryGetProperty("valor", out var vEl) && vEl.ValueKind == JsonValueKind.Number)
                    total += vEl.GetDecimal();
            }
            return temItem && total > 0;
        }
        catch
        {
            return false;
        }
    }

    private static List<AcaoPendencia> ParsearAcoesMarcadas(string conteudoJson)
    {
        if (string.IsNullOrWhiteSpace(conteudoJson))
            return [];

        JsonDocument doc;
        try { doc = JsonDocument.Parse(conteudoJson); }
        catch { return []; }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("conduta", out var conduta))
                return [];

            // Suporta tanto objeto { acoesMarcadas: [...] } quanto string legada (fallback)
            if (conduta.ValueKind != JsonValueKind.Object)
                return [];

            if (!conduta.TryGetProperty("acoesMarcadas", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return [];

            var resultado = new List<AcaoPendencia>();
            foreach (var item in arr.EnumerateArray())
            {
                var nome = item.GetString();
                if (!string.IsNullOrWhiteSpace(nome) &&
                    Enum.TryParse<AcaoPendencia>(nome, ignoreCase: false, out var acao))
                {
                    resultado.Add(acao);
                }
            }
            return resultado;
        }
    }
}
