using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Orcamentos;

[TestFixture]
public class CatalogosEstendidosTests
{
    [Test]
    public void CatalogoCirurgia_Criar_AceitaCamposNovosOpcionais()
    {
        var sut = CatalogoCirurgia.Criar(1, "Colecistectomia", 4800m, 90,
            codigoInterno: "INT-001", codigoTuss: "30912025", categoria: "Cirurgia geral");
        Assert.That(sut.CodigoInterno, Is.EqualTo("INT-001"));
        Assert.That(sut.CodigoTuss, Is.EqualTo("30912025"));
        Assert.That(sut.Categoria, Is.EqualTo("Cirurgia geral"));
    }

    [Test]
    public void CatalogoCirurgia_Criar_SemCamposNovos_DeixaNullos()
    {
        var sut = CatalogoCirurgia.Criar(1, "Apendicectomia", 3000m, 60);
        Assert.That(sut.CodigoInterno, Is.Null);
        Assert.That(sut.CodigoTuss, Is.Null);
        Assert.That(sut.Categoria, Is.Null);
    }

    [Test]
    public void CatalogoCirurgia_Atualizar_AceitaCamposNovos()
    {
        var sut = CatalogoCirurgia.Criar(1, "Hérnia", 3200m, 75);
        sut.Atualizar("Hérnia inguinal", 3500m, 80,
            codigoInterno: "INT-002", codigoTuss: "30713016", categoria: "Cirurgia geral");
        Assert.That(sut.Descricao, Is.EqualTo("Hérnia inguinal"));
        Assert.That(sut.Categoria, Is.EqualTo("Cirurgia geral"));
    }

    [Test]
    public void CatalogoCirurgia_CodigoTussMuitoLongo_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            CatalogoCirurgia.Criar(1, "X", 10m, 30, codigoTuss: new string('A', 21)));
    }

    [Test]
    public void CatalogoProduto_Criar_AceitaTipoEMarca()
    {
        var sut = CatalogoProduto.Criar(1, "Prótese mamária", null, 4800m, false,
            tipo: TipoOrcamentoProduto.OPME, marca: "Mentor", unidade: "un",
            fornecedorNome: "OPM Brasil", codigoSku: "OPM-001");
        Assert.That(sut.Tipo, Is.EqualTo(TipoOrcamentoProduto.OPME));
        Assert.That(sut.Marca, Is.EqualTo("Mentor"));
        Assert.That(sut.Unidade, Is.EqualTo("un"));
        Assert.That(sut.CodigoSku, Is.EqualTo("OPM-001"));
    }

    [Test]
    public void CatalogoProduto_Criar_SemTipo_DefaultaOutros()
    {
        var sut = CatalogoProduto.Criar(1, "Item", null, 10m, false);
        Assert.That(sut.Tipo, Is.EqualTo(TipoOrcamentoProduto.Outros));
        Assert.That(sut.Unidade, Is.EqualTo("un"));
    }

    [Test]
    public void CatalogoCirurgiaProduto_Criar_DefaultaIncluidoTrue()
    {
        var sut = CatalogoCirurgiaProduto.Criar(1, 2, 2m, obrigatorio: false);
        Assert.That(sut.Incluido, Is.True);
    }

    [Test]
    public void CatalogoCirurgiaProduto_Criar_IncluidoFalse_PreservaSemantica()
    {
        var sut = CatalogoCirurgiaProduto.Criar(1, 2, 2m, obrigatorio: false, incluido: false);
        Assert.That(sut.Incluido, Is.False);
    }

    [Test]
    public void CatalogoCirurgiaProduto_AtualizarQuantidade_AlteraIncluido()
    {
        var sut = CatalogoCirurgiaProduto.Criar(1, 2, 2m, obrigatorio: true, incluido: true);
        sut.AtualizarQuantidade(3m, obrigatorio: false, incluido: false);
        Assert.That(sut.QuantidadePadrao, Is.EqualTo(3m));
        Assert.That(sut.Obrigatorio, Is.False);
        Assert.That(sut.Incluido, Is.False);
    }
}
