using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Assinaturas;

[TestFixture]
public class PlanoTests
{
    [Test]
    public void TemFeature_PlanoComReceitasEIa_TemReceitasRetornaTrue()
    {
        var plano = Plano.Criar("Premium", 199m, null, null, ["receitas", "ia"]);

        Assert.That(plano.TemFeature("receitas"), Is.True);
    }

    [Test]
    public void TemFeature_PlanoComReceitasEIa_TemIaRetornaTrue()
    {
        var plano = Plano.Criar("Premium", 199m, null, null, ["receitas", "ia"]);

        Assert.That(plano.TemFeature("ia"), Is.True);
    }

    [Test]
    public void TemFeature_FeatureAusente_RetornaFalse()
    {
        var plano = Plano.Criar("Basico", 49m, null, null, ["receitas"]);

        Assert.That(plano.TemFeature("ia"), Is.False);
    }

    [Test]
    public void TemFeature_PlanoSemFeatures_RetornaFalse()
    {
        var plano = Plano.Criar("Free", 0m, null, null, null);

        Assert.That(plano.TemFeature("receitas"), Is.False);
    }

    [Test]
    public void TemFeature_ComparaCaseInsensitive_RetornaTrue()
    {
        var plano = Plano.Criar("Premium", 199m, null, null, ["Receitas"]);

        Assert.That(plano.TemFeature("RECEITAS"), Is.True);
    }

    [Test]
    public void TemFeature_FeatureVazia_RetornaFalse()
    {
        var plano = Plano.Criar("Premium", 199m, null, null, ["receitas"]);

        Assert.That(plano.TemFeature(""), Is.False);
    }

    [Test]
    public void Criar_LimiteProfissionaisNulo_Ilimitado()
    {
        var plano = Plano.Criar("Enterprise", 999m, null, null, null);

        Assert.That(plano.LimiteProfissionais, Is.Null);
    }

    [Test]
    public void Criar_ComLimiteProfissionais_LimiteDefinido()
    {
        var plano = Plano.Criar("Basico", 49m, 2, 50, null);

        Assert.That(plano.LimiteProfissionais, Is.EqualTo(2));
        Assert.That(plano.LimitePacientes, Is.EqualTo(50));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Plano.Criar("  ", 99m, null, null, null));

        Assert.That(ex.Message, Does.Contain("Nome do plano é obrigatório"));
    }

    [Test]
    public void Criar_PrecoNegativo_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Plano.Criar("Plano X", -1m, null, null, null));

        Assert.That(ex.Message, Does.Contain("Preço mensal não pode ser negativo"));
    }

    [Test]
    public void Inativar_PlanoAtivo_MudaParaInativo()
    {
        var plano = Plano.Criar("Basico", 49m, null, null, null);

        plano.Inativar();

        Assert.That(plano.Ativo, Is.False);
    }

    [Test]
    public void Inativar_PlanoJaInativo_LancaBusinessException()
    {
        var plano = Plano.Criar("Basico", 49m, null, null, null);
        plano.Inativar();

        var ex = Assert.Throws<BusinessException>(() => plano.Inativar());

        Assert.That(ex.Message, Does.Contain("já está inativo"));
    }
}
