using Dapper;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Npgsql;

namespace Imedto.Backend.Application.Orcamentos.Queries;

/// <summary>
/// Handler da consolidação de produtos. Lê vínculos cirurgia × produto +
/// catálogo de produtos do estabelecimento (filtrado por tenant), e roda o
/// <see cref="ProdutosConsolidador"/> puro para devolver a lista pronta.
/// </summary>
public class ConsolidarProdutosOrcamentoQueryHandler
    : IRequestHandler<ConsolidarProdutosOrcamentoQuery, List<ProdutoConsolidadoDto>>
{
    private readonly string _connStr;

    public ConsolidarProdutosOrcamentoQueryHandler(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<List<ProdutoConsolidadoDto>> Handle(ConsolidarProdutosOrcamentoQuery q)
    {
        var idsCirurgia = q.Cirurgias.Where(c => c.CatalogoCirurgiaId > 0)
                                     .Select(c => c.CatalogoCirurgiaId)
                                     .Distinct()
                                     .ToArray();
        if (idsCirurgia.Length == 0) return new List<ProdutoConsolidadoDto>();

        await using var conn = new NpgsqlConnection(_connStr);

        // 1. Vínculos cirurgia × produto — filtra por estabelecimento via JOIN com a cirurgia
        //    (defense-in-depth: ninguém de outro tenant consegue forçar produtos de outro estab).
        var vinculos = (await conn.QueryAsync<VinculoRow>(
            """
            SELECT cp.id AS Id, cp.catalogo_cirurgia_id AS CatalogoCirurgiaId,
                   cp.catalogo_produto_id AS CatalogoProdutoId,
                   cp.quantidade_padrao AS QuantidadePadrao,
                   cp.obrigatorio AS Obrigatorio, cp.incluido AS Incluido
            FROM orcamento_catalogo_cirurgia_produto cp
            JOIN orcamento_catalogo_cirurgia c ON c.id = cp.catalogo_cirurgia_id
            WHERE cp.catalogo_cirurgia_id = ANY(@Ids)
              AND c.estabelecimento_id = @Estab
            """,
            new { Ids = idsCirurgia, Estab = q.EstabelecimentoId })).ToList();

        if (vinculos.Count == 0) return new List<ProdutoConsolidadoDto>();

        var produtoIds = vinculos.Select(v => v.CatalogoProdutoId).Distinct().ToArray();

        // 2. Catálogo de produtos (tenant-filtered).
        var produtosRaw = await conn.QueryAsync<ProdutoRow>(
            """
            SELECT id AS Id, nome AS Nome, valor_referencia AS ValorReferencia,
                   uso_unico AS UsoUnico, ativo AS Ativo
            FROM orcamento_catalogo_produto
            WHERE id = ANY(@Ids) AND estabelecimento_id = @Estab
            """,
            new { Ids = produtoIds, Estab = q.EstabelecimentoId });

        // 3. Nomes das cirurgias para enriquecer a "origem".
        var nomesCirurgia = (await conn.QueryAsync<(long Id, string Descricao)>(
            "SELECT id, descricao FROM orcamento_catalogo_cirurgia WHERE id = ANY(@Ids) AND estabelecimento_id = @Estab",
            new { Ids = idsCirurgia, Estab = q.EstabelecimentoId }))
            .ToDictionary(r => r.Id, r => r.Descricao);

        // 4. Constrói entidades em memória para passar ao consolidador.
        var vinculosDomain = vinculos.Select(v =>
        {
            var e = CatalogoCirurgiaProduto.Criar(v.CatalogoCirurgiaId, v.CatalogoProdutoId, v.QuantidadePadrao, v.Obrigatorio, v.Incluido);
            return e;
        }).ToList();

        var produtosDomain = produtosRaw.Select(p =>
        {
            // Não passamos pela fábrica (que valida nome) para evitar overhead.
            // Usamos reflection mínima na ordem fields-of-Entity? Aqui só precisamos dos campos
            // expostos. A fábrica aceita esses parâmetros sem efeito colateral relevante.
            return CatalogoProduto.Criar(q.EstabelecimentoId, p.Nome, descricao: null,
                valorReferencia: p.ValorReferencia, usoUnico: p.UsoUnico);
        }).ToList();
        // Como a fábrica gera Id=0, mapeamos por nome (ids reais via dicionário paralelo).
        // Simplifica usar um dicionário direto p/ não recriar entidades só por causa de Id.
        var produtosLookup = produtosRaw.ToDictionary(p => p.Id, p => new ProdutoCalc(p.Id, p.Nome, p.ValorReferencia, p.UsoUnico, p.Ativo));

        // Reimplementação direta da regra (espelha ProdutosConsolidador) sem precisar criar
        // CatalogoProduto via fábrica — evita acoplamento ao construtor da entidade no read-side.
        var acc = new Dictionary<long, ProdutoConsolidadoDto>();
        foreach (var cirurgia in q.Cirurgias)
        {
            if (cirurgia.Quantidade <= 0) continue;
            var vs = vinculos.Where(v => v.CatalogoCirurgiaId == cirurgia.CatalogoCirurgiaId);
            foreach (var v in vs)
            {
                if (!v.Incluido) continue;
                if (!produtosLookup.TryGetValue(v.CatalogoProdutoId, out var prod)) continue;
                if (!prod.Ativo) continue;

                var qtdEfetiva = v.QuantidadePadrao * cirurgia.Quantidade;
                var nomeCir = nomesCirurgia.GetValueOrDefault(cirurgia.CatalogoCirurgiaId) ?? "";

                if (!acc.TryGetValue(prod.Id, out var existente))
                {
                    acc[prod.Id] = new ProdutoConsolidadoDto
                    {
                        ProdutoId = prod.Id,
                        ProdutoNome = prod.Nome,
                        Quantidade = qtdEfetiva,
                        ValorUnitario = prod.ValorReferencia ?? 0m,
                        Subtotal = Math.Round(qtdEfetiva * (prod.ValorReferencia ?? 0m), 2),
                        UsoUnico = prod.UsoUnico,
                        OrigemCirurgiaIds = new List<long> { cirurgia.CatalogoCirurgiaId },
                        OrigemCirurgiaNomes = new List<string> { nomeCir },
                    };
                    continue;
                }

                existente.Quantidade = prod.UsoUnico
                    ? Math.Max(existente.Quantidade, qtdEfetiva)
                    : existente.Quantidade + qtdEfetiva;
                existente.Subtotal = Math.Round(existente.Quantidade * existente.ValorUnitario, 2);

                if (!existente.OrigemCirurgiaIds.Contains(cirurgia.CatalogoCirurgiaId))
                {
                    existente.OrigemCirurgiaIds.Add(cirurgia.CatalogoCirurgiaId);
                    if (!string.IsNullOrEmpty(nomeCir))
                        existente.OrigemCirurgiaNomes.Add(nomeCir);
                }
            }
        }

        return acc.Values.OrderBy(p => p.ProdutoNome).ToList();
    }

    private record VinculoRow(long Id, long CatalogoCirurgiaId, long CatalogoProdutoId,
        decimal QuantidadePadrao, bool Obrigatorio, bool Incluido);
    private record ProdutoRow(long Id, string Nome, decimal? ValorReferencia, bool UsoUnico, bool Ativo);
    private record ProdutoCalc(long Id, string Nome, decimal? ValorReferencia, bool UsoUnico, bool Ativo);
}
