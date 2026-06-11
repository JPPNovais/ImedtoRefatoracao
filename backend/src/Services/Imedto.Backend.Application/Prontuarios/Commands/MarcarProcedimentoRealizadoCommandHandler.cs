using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Fluxo F4 — MarcarProcedimentoRealizado (briefing 2026-06-10_013 + addendum).
///
/// Atomicidade total (UoW do controller):
///   1. Valida pendência (status Pendente, ação MarcarProcedimentoRealizado) — idempotência de domínio.
///   2. Carrega evolução via prontuário do paciente (multi-tenant defense-in-depth).
///   3. Lê procedimentos-indicados do ConteudoJson — lança 422 se vazio (D5/CA80).
///   4. Cria Cobrança origem=Procedimento, tipo=Particular, valor=soma snapshots (R2/D1).
///   5. Salva cobrança (EvolucaoId + índice UNIQUE parcial = defense-in-depth idempotência D4/CA77).
///   6. Resolve produtos via JOIN cirurgia×produto (Dapper batched, sem N+1 CA85).
///      Para cada produto com ItemInventarioId, chama RegistrarSaida (estoque insuficiente → BusinessException R8/CA78).
///   7. Conclui pendência com referencia_id = cobranca.Id (R-conclusão/CA76).
///
/// LGPD: Cobranca.Descricao e MovimentacaoEstoque.Observacao não carregam PII (R9/CA83).
/// RBAC: controller exige prontuario.editar (R12/D9/CA82).
/// Multi-tenant: todo acesso filtrado por estabelecimentoId (R11/CA81).
/// </summary>
public class MarcarProcedimentoRealizadoCommandHandler : ICommandHandler<MarcarProcedimentoRealizadoCommand>
{
    private readonly IPendenciaAtendimentoRepository _pendenciaRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioEvolucaoRepository _evolucaoRepo;
    private readonly ICobrancaRepository _cobrancaRepo;
    private readonly IItemInventarioRepository _invRepo;
    private readonly IMovimentacaoEstoqueRepository _movRepo;
    private readonly string _connStr;

    public MarcarProcedimentoRealizadoCommandHandler(
        IPendenciaAtendimentoRepository pendenciaRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioEvolucaoRepository evolucaoRepo,
        ICobrancaRepository cobrancaRepo,
        IItemInventarioRepository invRepo,
        IMovimentacaoEstoqueRepository movRepo,
        AppReadConnectionString conn)
    {
        _pendenciaRepo = pendenciaRepo;
        _prontuarioRepo = prontuarioRepo;
        _evolucaoRepo = evolucaoRepo;
        _cobrancaRepo = cobrancaRepo;
        _invRepo = invRepo;
        _movRepo = movRepo;
        _connStr = conn.Value;
    }

    public async Task Handle(MarcarProcedimentoRealizadoCommand command)
    {
        // ── 1. Pendência ─────────────────────────────────────────────────────────
        var pendencia = await _pendenciaRepo.ObterPorId(command.PendenciaId, command.EstabelecimentoId)
            ?? throw new BusinessException("Pendência não encontrada.");

        if (pendencia.Acao != AcaoPendencia.MarcarProcedimentoRealizado)
            throw new BusinessException("Esta pendência não é do tipo 'Marcar procedimento realizado'.");

        // Idempotência de domínio (D4/CA77): pendência já concluída → no-op silencioso.
        if (pendencia.Status == StatusPendencia.Concluida)
            return;

        // ── 2. Evolução (multi-tenant via prontuário do paciente) ─────────────────
        var prontuario = await _prontuarioRepo.ObterPorPaciente(pendencia.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Não encontrado.");

        var evolucao = await _evolucaoRepo.ObterDoProntuarioOuNulo(pendencia.EvolucaoId, prontuario.Id)
            ?? throw new BusinessException("Não encontrado.");

        // ── 3. Procedimentos indicados do ConteudoJson ───────────────────────────
        var procedimentos = ExtrairProcedimentosIndicados(evolucao.ConteudoJson);
        if (procedimentos.Count == 0)
            throw new BusinessException("Esta evolução não tem procedimentos indicados para marcar como realizado.");

        var valorTotal = procedimentos.Sum(p => p.Valor);
        if (valorTotal <= 0)
            throw new BusinessException("Esta evolução não tem procedimentos indicados para marcar como realizado.");

        // Descrição de cobrança sem PII (R9/CA83).
        var descricaoCobranca = procedimentos.Count == 1
            ? $"Procedimento realizado: {procedimentos[0].Descricao}"
            : $"Procedimentos realizados ({procedimentos.Count})";

        // ── 4. Cobrança ────────────────────────────────────────────────────────────
        var cobranca = Cobranca.CriarParaProcedimento(
            command.EstabelecimentoId,
            pendencia.PacienteId,
            evolucao.Id,
            pendencia.AgendamentoId,
            valorTotal,
            descricaoCobranca,
            command.UsuarioId);

        await _cobrancaRepo.Salvar(cobranca);
        // MarcarComoCriada não é chamado aqui — o domain event será disparado pelo controller após UoW
        // (padrão F1: CobrancaCriadaEvent é opcional para o badge; o badge já lista por paciente_id).

        // ── 5. Baixa de estoque (batched, sem N+1) ────────────────────────────────
        var cirurgiaIds = procedimentos
            .Select(p => p.CatalogoCirurgiaId)
            .Distinct()
            .ToArray();

        if (cirurgiaIds.Length > 0)
        {
            var produtosConsolidados = await ResolverProdutosParaBaixa(
                cirurgiaIds, procedimentos, command.EstabelecimentoId);

            foreach (var prod in produtosConsolidados)
            {
                if (prod.ItemInventarioId is null) continue; // sem vínculo → ignora (R5/CA94)

                var item = await _invRepo.ObterPorIdOuNulo(prod.ItemInventarioId.Value, command.EstabelecimentoId)
                    ?? throw new BusinessException("Não encontrado.");

                // RegistrarSaida lança BusinessException se estoque insuficiente (R8/CA78 → rollback total via UoW).
                // F7/R21: cobrancaId gravado para rastreabilidade de custo por paciente (sem parse de observação).
                var observacao = $"Baixa automática — procedimento realizado, cobrança #{cobranca.Id}";
                var mov = item.RegistrarSaida(prod.Quantidade, command.UsuarioId, observacao, cobranca.Id);
                await _movRepo.Salvar(mov);
            }
        }

        // ── 6. Conclui pendência ──────────────────────────────────────────────────
        pendencia.ConcluirPorGatilho(cobranca.Id);

        command.CobrancaIdGerada = cobranca.Id;
    }

    // ── Parsing do ConteudoJson ───────────────────────────────────────────────────

    /// <summary>
    /// Extrai a seção "procedimentos-indicados" do ConteudoJson da evolução.
    /// Formato novo (F3): { procedimentos: [ { catalogoCirurgiaId, descricao, valor, observacao } ] }.
    /// Itens sem catalogoCirurgiaId (legado texto-livre) são ignorados na cobrança.
    /// </summary>
    private static List<ProcedimentoIndicadoItem> ExtrairProcedimentosIndicados(string conteudoJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(conteudoJson);
            if (!doc.RootElement.TryGetProperty("procedimentos-indicados", out var secao))
                return new List<ProcedimentoIndicadoItem>();

            if (!secao.TryGetProperty("procedimentos", out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return new List<ProcedimentoIndicadoItem>();

            var resultado = new List<ProcedimentoIndicadoItem>();
            foreach (var item in arr.EnumerateArray())
            {
                if (!item.TryGetProperty("catalogoCirurgiaId", out var idEl)) continue;
                if (idEl.ValueKind == JsonValueKind.Null) continue;

                var catalogoCirurgiaId = idEl.GetInt64();
                var descricao = item.TryGetProperty("descricao", out var dEl) ? dEl.GetString() ?? "" : "";
                var valor = item.TryGetProperty("valor", out var vEl) && vEl.ValueKind == JsonValueKind.Number
                    ? vEl.GetDecimal() : 0m;
                var observacao = item.TryGetProperty("observacao", out var oEl) ? oEl.GetString() : null;

                resultado.Add(new ProcedimentoIndicadoItem(catalogoCirurgiaId, descricao, valor, observacao));
            }
            return resultado;
        }
        catch
        {
            return new List<ProcedimentoIndicadoItem>();
        }
    }

    /// <summary>
    /// Resolve produtos a baixar via query Dapper batched (CA85 — sem N+1).
    /// Aplica a regra ProdutosConsolidador: UsoUnico=MAX, múltiplo=SOMA.
    /// Cada procedimento conta como 1 ocorrência do catalogoCirurgiaId (R4).
    /// Retorna apenas produtos com ItemInventarioId setado que participam da baixa.
    /// </summary>
    private async Task<List<ProdutoParaBaixa>> ResolverProdutosParaBaixa(
        long[] cirurgiaIds,
        List<ProcedimentoIndicadoItem> procedimentos,
        long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Vínculos cirurgia×produto filtrados por tenant (via JOIN na cirurgia).
        var vinculos = (await conn.QueryAsync<VinculoRow>(
            """
            SELECT cp.catalogo_produto_id AS CatalogoProdutoId,
                   cp.catalogo_cirurgia_id AS CatalogoCirurgiaId,
                   cp.quantidade_padrao AS QuantidadePadrao,
                   cp.incluido AS Incluido,
                   p.uso_unico AS UsoUnico,
                   p.item_inventario_id AS ItemInventarioId
            FROM orcamento_catalogo_cirurgia_produto cp
            JOIN orcamento_catalogo_cirurgia c ON c.id = cp.catalogo_cirurgia_id
            JOIN orcamento_catalogo_produto p ON p.id = cp.catalogo_produto_id
            WHERE cp.catalogo_cirurgia_id = ANY(@Ids)
              AND c.estabelecimento_id = @Estab
              AND p.estabelecimento_id = @Estab
              AND p.ativo = true
            """,
            new { Ids = cirurgiaIds, Estab = estabelecimentoId })).ToList();

        if (vinculos.Count == 0) return new List<ProdutoParaBaixa>();

        // Consolida usando a regra ProdutosConsolidador: cada procedimento-indicado conta como 1 ocorrência.
        // Procedimentos repetidos (mesmo catalogoCirurgiaId) somam ocorrências.
        var ocorrenciasPorCirurgia = procedimentos
            .GroupBy(p => p.CatalogoCirurgiaId)
            .ToDictionary(g => g.Key, g => g.Count());

        var acc = new Dictionary<long, ProdutoParaBaixa>();

        foreach (var (cirurgiaId, qtdOcorrencias) in ocorrenciasPorCirurgia)
        {
            var vs = vinculos.Where(v => v.CatalogoCirurgiaId == cirurgiaId);
            foreach (var v in vs)
            {
                if (!v.Incluido) continue;

                var qtdEfetiva = v.QuantidadePadrao * qtdOcorrencias;

                if (!acc.TryGetValue(v.CatalogoProdutoId, out var existente))
                {
                    acc[v.CatalogoProdutoId] = new ProdutoParaBaixa(
                        v.CatalogoProdutoId, qtdEfetiva, v.UsoUnico, v.ItemInventarioId);
                    continue;
                }

                existente.Quantidade = v.UsoUnico
                    ? Math.Max(existente.Quantidade, qtdEfetiva)
                    : existente.Quantidade + qtdEfetiva;
            }
        }

        return acc.Values.ToList();
    }

    private record ProcedimentoIndicadoItem(
        long CatalogoCirurgiaId,
        string Descricao,
        decimal Valor,
        string? Observacao);

    private record VinculoRow(
        long CatalogoProdutoId,
        long CatalogoCirurgiaId,
        decimal QuantidadePadrao,
        bool Incluido,
        bool UsoUnico,
        long? ItemInventarioId);

    private class ProdutoParaBaixa
    {
        public ProdutoParaBaixa(long produtoId, decimal quantidade, bool usoUnico, long? itemInventarioId)
        {
            ProdutoId = produtoId;
            Quantidade = quantidade;
            UsoUnico = usoUnico;
            ItemInventarioId = itemInventarioId;
        }
        public long ProdutoId { get; }
        public decimal Quantidade { get; set; }
        public bool UsoUnico { get; }
        public long? ItemInventarioId { get; }
    }
}
