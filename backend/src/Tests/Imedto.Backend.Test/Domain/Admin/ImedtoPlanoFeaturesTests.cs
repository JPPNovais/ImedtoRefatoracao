using Imedto.Backend.Domain.Admin;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre a propriedade FeaturesJson no ImedtoPlano (briefing 2026-06-11_003, F1 CA3).
/// </summary>
[TestFixture]
public class ImedtoPlanoFeaturesTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    [Test]
    public void Criar_SemFeaturesJson_DefaultVazio()
    {
        var plano = ImedtoPlano.Criar("Plano Básico", null, null, false, "{}", _adminId);

        Assert.That(plano.FeaturesJson, Is.EqualTo("{}"));
    }

    [Test]
    public void Criar_ComFeaturesJson_ArmazenaCorretamente()
    {
        var features = """{"receitas":true,"ia":false}""";
        var plano = ImedtoPlano.Criar("Plano Pro", null, 9900, false, "{}", _adminId, featuresJson: features);

        Assert.That(plano.FeaturesJson, Is.EqualTo(features));
    }

    [Test]
    public void Criar_FeaturesJsonVazio_NormalizaParaChaves()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId, featuresJson: "   ");

        Assert.That(plano.FeaturesJson, Is.EqualTo("{}"));
    }

    [Test]
    public void Atualizar_ComFeaturesJson_AtualizaCorretamente()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId);
        var novasFeatures = """{"receitas":true,"ia":true,"relatorios_avancados":false}""";

        plano.Atualizar("Plano X", null, null, false, "{}", featuresJson: novasFeatures);

        Assert.That(plano.FeaturesJson, Is.EqualTo(novasFeatures));
    }

    [Test]
    public void Atualizar_SemFeaturesJson_MantemChavesVazias()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId,
            featuresJson: """{"receitas":true}""");

        // Não passa featuresJson — default "{}"
        plano.Atualizar("Plano X", null, null, false, "{}");

        Assert.That(plano.FeaturesJson, Is.EqualTo("{}"));
    }
}
