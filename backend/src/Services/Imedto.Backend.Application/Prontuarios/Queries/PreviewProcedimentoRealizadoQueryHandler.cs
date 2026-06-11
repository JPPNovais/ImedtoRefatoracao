using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Preview do modal MarcarProcedimentoRealizado (GET, sem persistência).
/// Retorna procedimentos + valor total + produtos a baixar (com sinalização de sem-vínculo).
/// Multi-tenant via cadeia pendência → prontuário → estabelecimentoId.
/// </summary>
public class PreviewProcedimentoRealizadoQueryHandler
    : IRequestHandler<PreviewProcedimentoRealizadoQuery, PreviewProcedimentoRealizadoDto>
{
    private readonly IPendenciaAtendimentoRepository _pendenciaRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioEvolucaoRepository _evolucaoRepo;
    private readonly string _connStr;

    public PreviewProcedimentoRealizadoQueryHandler(
        IPendenciaAtendimentoRepository pendenciaRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioEvolucaoRepository evolucaoRepo,
        AppReadConnectionString conn)
    {
        _pendenciaRepo = pendenciaRepo;
        _prontuarioRepo = prontuarioRepo;
        _evolucaoRepo = evolucaoRepo;
        _connStr = conn.Value;
    }

    public async Task<PreviewProcedimentoRealizadoDto> Handle(PreviewProcedimentoRealizadoQuery q)
    {
        var pendencia = await _pendenciaRepo.ObterPorId(q.PendenciaId, q.EstabelecimentoId)
            ?? throw new BusinessException("Pendência não encontrada.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(pendencia.PacienteId, q.EstabelecimentoId)
            ?? throw new BusinessException("Não encontrado.");

        var evolucao = await _evolucaoRepo.ObterDoProntuarioOuNulo(pendencia.EvolucaoId, prontuario.Id)
            ?? throw new BusinessException("Não encontrado.");

        var procedimentos = ExtrairProcedimentosIndicados(evolucao.ConteudoJson);

        var dto = new PreviewProcedimentoRealizadoDto
        {
            PendenciaId = pendencia.Id,
            EvolucaoId = evolucao.Id,
            Procedimentos = procedimentos.Select(p => new ProcedimentoPreviewItem
            {
                CatalogoCirurgiaId = p.CatalogoCirurgiaId,
                Descricao = p.Descricao,
                Valor = p.Valor,
                Observacao = p.Observacao,
            }).ToList(),
            ValorTotal = Domain.Cobrancas.ArredondamentoMonetario.Arredondar(procedimentos.Sum(p => p.Valor)),
        };

        if (procedimentos.Count > 0)
        {
            var cirurgiaIds = procedimentos
                .Where(p => p.CatalogoCirurgiaId > 0)
                .Select(p => p.CatalogoCirurgiaId)
                .Distinct()
                .ToArray();

            if (cirurgiaIds.Length > 0)
            {
                var produtos = await ResolverProdutosPreview(
                    cirurgiaIds, procedimentos, q.EstabelecimentoId);
                dto.ProdutosABaixar = produtos;
                dto.TemProdutoSemVinculo = produtos.Any(p => p.SemVinculo);
            }
        }

        return dto;
    }

    private static List<ProcItem> ExtrairProcedimentosIndicados(string conteudoJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(conteudoJson);
            if (!doc.RootElement.TryGetProperty("procedimentos-indicados", out var secao)) return new();
            if (!secao.TryGetProperty("procedimentos", out var arr) || arr.ValueKind != JsonValueKind.Array) return new();

            var resultado = new List<ProcItem>();
            foreach (var item in arr.EnumerateArray())
            {
                if (!item.TryGetProperty("catalogoCirurgiaId", out var idEl) || idEl.ValueKind == JsonValueKind.Null) continue;
                var catalogoCirurgiaId = idEl.GetInt64();
                var descricao = item.TryGetProperty("descricao", out var dEl) ? dEl.GetString() ?? "" : "";
                var valor = item.TryGetProperty("valor", out var vEl) && vEl.ValueKind == JsonValueKind.Number ? vEl.GetDecimal() : 0m;
                var observacao = item.TryGetProperty("observacao", out var oEl) ? oEl.GetString() : null;
                resultado.Add(new ProcItem(catalogoCirurgiaId, descricao, valor, observacao));
            }
            return resultado;
        }
        catch { return new(); }
    }

    private async Task<List<ProdutoPreviewItem>> ResolverProdutosPreview(
        long[] cirurgiaIds,
        List<ProcItem> procedimentos,
        long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var vinculos = (await conn.QueryAsync<VinculoPreviewRow>(
            """
            SELECT cp.catalogo_produto_id AS ProdutoId,
                   cp.catalogo_cirurgia_id AS CatalogoCirurgiaId,
                   cp.quantidade_padrao AS QuantidadePadrao,
                   cp.incluido AS Incluido,
                   p.nome AS ProdutoNome,
                   p.uso_unico AS UsoUnico,
                   p.item_inventario_id AS ItemInventarioId,
                   i.nome AS ItemInventarioNome
            FROM orcamento_catalogo_cirurgia_produto cp
            JOIN orcamento_catalogo_cirurgia c ON c.id = cp.catalogo_cirurgia_id
            JOIN orcamento_catalogo_produto p ON p.id = cp.catalogo_produto_id
            LEFT JOIN itens_inventario i ON i.id = p.item_inventario_id
            WHERE cp.catalogo_cirurgia_id = ANY(@Ids)
              AND c.estabelecimento_id = @Estab
              AND p.estabelecimento_id = @Estab
              AND p.ativo = true
            """,
            new { Ids = cirurgiaIds, Estab = estabelecimentoId })).ToList();

        if (vinculos.Count == 0) return new();

        var ocorrenciasPorCirurgia = procedimentos
            .GroupBy(p => p.CatalogoCirurgiaId)
            .ToDictionary(g => g.Key, g => g.Count());

        var acc = new Dictionary<long, ProdutoPreviewItem>();

        foreach (var (cirurgiaId, qtdOcorrencias) in ocorrenciasPorCirurgia)
        {
            var vs = vinculos.Where(v => v.CatalogoCirurgiaId == cirurgiaId);
            foreach (var v in vs)
            {
                if (!v.Incluido) continue;
                var qtdEfetiva = v.QuantidadePadrao * qtdOcorrencias;

                if (!acc.TryGetValue(v.ProdutoId, out var existente))
                {
                    acc[v.ProdutoId] = new ProdutoPreviewItem
                    {
                        ProdutoId = v.ProdutoId,
                        ProdutoNome = v.ProdutoNome,
                        Quantidade = qtdEfetiva,
                        ItemInventarioId = v.ItemInventarioId,
                        ItemInventarioNome = v.ItemInventarioNome,
                        SemVinculo = v.ItemInventarioId is null,
                    };
                    continue;
                }

                existente.Quantidade = v.UsoUnico
                    ? Math.Max(existente.Quantidade, qtdEfetiva)
                    : existente.Quantidade + qtdEfetiva;
            }
        }

        return acc.Values.OrderBy(p => p.ProdutoNome).ToList();
    }

    private record ProcItem(long CatalogoCirurgiaId, string Descricao, decimal Valor, string? Observacao);
    private record VinculoPreviewRow(
        long ProdutoId, long CatalogoCirurgiaId, decimal QuantidadePadrao, bool Incluido,
        string ProdutoNome, bool UsoUnico, long? ItemInventarioId, string? ItemInventarioNome);
}
