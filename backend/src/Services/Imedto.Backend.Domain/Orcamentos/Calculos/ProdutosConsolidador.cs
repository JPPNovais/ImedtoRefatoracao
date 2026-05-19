using Imedto.Backend.Domain.Orcamentos.Catalogos;

namespace Imedto.Backend.Domain.Orcamentos.Calculos;

/// <summary>
/// Consolidação de produtos entre N cirurgias de um orçamento — espelha a regra
/// do legado <c>useOrcamentoProdutosConsolidados.ts</c>:
///
/// <list type="bullet">
///   <item><c>UsoUnico = true</c>: pega a MAIOR quantidade entre as cirurgias
///         (ex: kit cirúrgico que serve para várias cirurgias na mesma sessão).</item>
///   <item><c>UsoUnico = false</c>: SOMA as quantidades — quantidade do vínculo
///         multiplicada pela quantidade da cirurgia no orçamento.</item>
/// </list>
///
/// Serviço puro: recebe lista de cirurgias selecionadas + vínculos de produtos +
/// metadados de produto. Retorna a lista consolidada. Sem efeito colateral.
/// </summary>
public static class ProdutosConsolidador
{
    /// <param name="cirurgiasSelecionadas">
    /// Lista de cirurgias do orçamento (catálogo). Cada item contém o id de
    /// catálogo e a quantidade no orçamento.
    /// </param>
    /// <param name="vinculos">
    /// Vínculos cirurgia × produto carregados do catálogo. Cada vínculo cita
    /// <c>CatalogoCirurgiaId</c>, <c>CatalogoProdutoId</c> e <c>QuantidadePadrao</c>.
    /// </param>
    /// <param name="produtos">Catálogo de produtos (lookup por id).</param>
    public static List<ProdutoConsolidadoResultado> Consolidar(
        IEnumerable<CirurgiaSelecionada> cirurgiasSelecionadas,
        IEnumerable<CatalogoCirurgiaProduto> vinculos,
        IEnumerable<CatalogoProduto> produtos)
    {
        var produtosLookup = produtos.ToDictionary(p => p.Id);
        var vinculosPorCirurgia = vinculos
            .GroupBy(v => v.CatalogoCirurgiaId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var acumulador = new Dictionary<long, ProdutoConsolidadoResultado>();

        foreach (var cirurgia in cirurgiasSelecionadas)
        {
            if (cirurgia.Quantidade <= 0) continue;
            if (!vinculosPorCirurgia.TryGetValue(cirurgia.CatalogoCirurgiaId, out var vs)) continue;

            foreach (var v in vs)
            {
                if (!v.Incluido) continue;
                if (!produtosLookup.TryGetValue(v.CatalogoProdutoId, out var produto)) continue;
                if (!produto.Ativo) continue;

                // Quantidade efetiva: padrão do vínculo × quantidade da cirurgia.
                var quantidadeEfetiva = v.QuantidadePadrao * cirurgia.Quantidade;

                if (!acumulador.TryGetValue(produto.Id, out var consolidado))
                {
                    acumulador[produto.Id] = new ProdutoConsolidadoResultado(
                        ProdutoId: produto.Id,
                        ProdutoNome: produto.Nome,
                        Quantidade: quantidadeEfetiva,
                        ValorUnitario: produto.ValorReferencia ?? 0m,
                        UsoUnico: produto.UsoUnico,
                        OrigemCirurgiaIds: new List<long> { cirurgia.CatalogoCirurgiaId });
                    continue;
                }

                // Já existe — aplica regra de consolidação.
                var novaQuantidade = produto.UsoUnico
                    ? Math.Max(consolidado.Quantidade, quantidadeEfetiva)
                    : consolidado.Quantidade + quantidadeEfetiva;

                var origens = consolidado.OrigemCirurgiaIds.ToList();
                if (!origens.Contains(cirurgia.CatalogoCirurgiaId))
                    origens.Add(cirurgia.CatalogoCirurgiaId);

                acumulador[produto.Id] = consolidado with
                {
                    Quantidade = novaQuantidade,
                    OrigemCirurgiaIds = origens,
                };
            }
        }

        return acumulador.Values.ToList();
    }
}

/// <summary>Cirurgia selecionada no orçamento (catálogo + quantidade).</summary>
public record CirurgiaSelecionada(long CatalogoCirurgiaId, int Quantidade);

/// <summary>Resultado de uma linha consolidada (1 por produto distinto).</summary>
public record ProdutoConsolidadoResultado(
    long ProdutoId,
    string ProdutoNome,
    decimal Quantidade,
    decimal ValorUnitario,
    bool UsoUnico,
    IReadOnlyList<long> OrigemCirurgiaIds);
