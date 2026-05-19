using Imedto.Backend.Domain.Orcamentos.Calculos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class ProdutosConsolidadorTests
{
    private static CatalogoProduto Produto(string nome, bool usoUnico, decimal? valor = 100m)
        => CatalogoProduto.Criar(estabelecimentoId: 1, nome: nome, descricao: null,
            valorReferencia: valor, usoUnico: usoUnico);

    private static CatalogoCirurgiaProduto Vinculo(long cirurgiaId, long produtoId, decimal qtd)
        => CatalogoCirurgiaProduto.Criar(cirurgiaId, produtoId, qtd, obrigatorio: false, incluido: true);

    [Test]
    public void Consolidar_UsoUnico_PegaMaiorQuantidadeEntreCirurgias()
    {
        // CA-2: dois orçamentos de cirurgias diferentes têm um vínculo do MESMO produto
        // (uso único). Deve aparecer 1 linha com a MAIOR quantidade.
        var produto = Produto("Kit cirúrgico", usoUnico: true);
        // Hack: como Id é set pela infra, precisamos atribuir manualmente para o teste.
        SetId(produto, 10);
        var prodA = produto;
        var v1 = Vinculo(cirurgiaId: 1, produtoId: 10, qtd: 2);
        var v2 = Vinculo(cirurgiaId: 2, produtoId: 10, qtd: 5);
        var cirurgias = new[]
        {
            new CirurgiaSelecionada(CatalogoCirurgiaId: 1, Quantidade: 1),
            new CirurgiaSelecionada(CatalogoCirurgiaId: 2, Quantidade: 1),
        };

        var consolidado = ProdutosConsolidador.Consolidar(cirurgias, new[] { v1, v2 }, new[] { prodA });

        Assert.That(consolidado, Has.Count.EqualTo(1));
        Assert.That(consolidado[0].Quantidade, Is.EqualTo(5m), "uso único pega o MAX (5), não soma");
    }

    [Test]
    public void Consolidar_NaoUsoUnico_SomaQuantidadesEntreCirurgias()
    {
        // CA-2 inverso: uso_unico=false → soma quantidades × quantidade de cada cirurgia.
        var produto = Produto("Compressa", usoUnico: false);
        SetId(produto, 20);
        var v1 = Vinculo(cirurgiaId: 1, produtoId: 20, qtd: 3);
        var v2 = Vinculo(cirurgiaId: 2, produtoId: 20, qtd: 4);
        var cirurgias = new[]
        {
            new CirurgiaSelecionada(1, 1),
            new CirurgiaSelecionada(2, 2), // 2 unidades dessa cirurgia → 4×2 = 8
        };

        var consolidado = ProdutosConsolidador.Consolidar(cirurgias, new[] { v1, v2 }, new[] { produto });

        Assert.That(consolidado, Has.Count.EqualTo(1));
        Assert.That(consolidado[0].Quantidade, Is.EqualTo(3 + 4 * 2));
    }

    [Test]
    public void Consolidar_VinculoNaoIncluido_IgnoraProduto()
    {
        var produto = Produto("Adesivo cirúrgico", usoUnico: false);
        SetId(produto, 30);
        var vinculo = CatalogoCirurgiaProduto.Criar(1, 30, 5, obrigatorio: false, incluido: false);
        var cirurgias = new[] { new CirurgiaSelecionada(1, 1) };

        var consolidado = ProdutosConsolidador.Consolidar(cirurgias, new[] { vinculo }, new[] { produto });

        Assert.That(consolidado, Is.Empty);
    }

    [Test]
    public void Consolidar_ProdutoInativo_IgnoraSilenciosamente()
    {
        var produto = Produto("Sutura", usoUnico: false);
        SetId(produto, 40);
        produto.Inativar();
        var vinculo = Vinculo(1, 40, 1);

        var consolidado = ProdutosConsolidador.Consolidar(
            new[] { new CirurgiaSelecionada(1, 1) }, new[] { vinculo }, new[] { produto });

        Assert.That(consolidado, Is.Empty);
    }

    /// <summary>Ajusta o Id da entidade via reflection (a fábrica gera Id=0; setter é protected).</summary>
    private static void SetId<T>(T entity, long id) where T : Imedto.Backend.SharedKernel.Domain.Entity
    {
        var prop = typeof(Imedto.Backend.SharedKernel.Domain.Entity<long>)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!;
        var setter = prop.GetSetMethod(nonPublic: true)!;
        setter.Invoke(entity, new object[] { id });
    }
}
