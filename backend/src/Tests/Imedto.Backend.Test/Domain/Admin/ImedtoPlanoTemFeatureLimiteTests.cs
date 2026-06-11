using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre ImedtoPlano.TemFeature() e ObterLimiteProfissionais/ObterLimitePacientes (briefing 2026-06-11_003 F3).
/// FeaturesJson usa formato {"ia": true, "receitas": false} — dict de bool.
/// LimitesJson usa formato {"profissionais": 5, "pacientes": 100} — valores int ou null.
/// </summary>
[TestFixture]
public class ImedtoPlanoTemFeatureLimiteTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    // --------------------------------------------------
    // TemFeature — formato dict bool {"ia": true/false}
    // --------------------------------------------------

    [Test]
    public void TemFeature_FeaturesVazio_RetornaFalse()
    {
        var plano = PlanoComFeatures("{}");
        Assert.That(plano.TemFeature(Features.Ia), Is.False);
    }

    [Test]
    public void TemFeature_FeatureHabilitada_RetornaTrue()
    {
        var plano = PlanoComFeatures("""{"ia":true}""");
        Assert.That(plano.TemFeature(Features.Ia), Is.True);
    }

    [Test]
    public void TemFeature_FeatureDesabilitada_RetornaFalse()
    {
        var plano = PlanoComFeatures("""{"ia":false}""");
        Assert.That(plano.TemFeature(Features.Ia), Is.False);
    }

    [Test]
    public void TemFeature_FeatureAusenteNoJson_RetornaFalse()
    {
        var plano = PlanoComFeatures("""{"receitas":true}""");
        Assert.That(plano.TemFeature(Features.Ia), Is.False);
    }

    [Test]
    public void TemFeature_CompareCaseInsensitive_RetornaTrue()
    {
        // Chave com maiúscula no JSON deve funcionar case-insensitive.
        var plano = PlanoComFeatures("""{"IA":true}""");
        Assert.That(plano.TemFeature("ia"), Is.True);
        Assert.That(plano.TemFeature("IA"), Is.True);
    }

    [Test]
    public void TemFeature_JsonMalformado_RetornaFalse_FailClosed()
    {
        var plano = PlanoComFeatures("nao_e_json");
        Assert.That(plano.TemFeature(Features.Ia), Is.False);
    }

    [Test]
    public void TemFeature_TodasAsFeaturesTrueGratuidadeVitalicia_RetornaTrue()
    {
        // Gratuidade Vitalícia tem todas as features habilitadas (seed F1).
        var featuresJson = """
            {
                "receitas": true,
                "exame_fisico": true,
                "procedimentos_cirurgicos": true,
                "orcamento_completo": true,
                "ia": true,
                "relatorios_avancados": true,
                "automacoes_ilimitadas": true,
                "anexos_ilimitados": true
            }
            """;
        var plano = PlanoComFeatures(featuresJson);

        Assert.That(plano.TemFeature(Features.Receitas), Is.True);
        Assert.That(plano.TemFeature(Features.ExameFisico), Is.True);
        Assert.That(plano.TemFeature(Features.ProcedimentosCirurgicos), Is.True);
        Assert.That(plano.TemFeature(Features.OrcamentoCompleto), Is.True);
        Assert.That(plano.TemFeature(Features.Ia), Is.True);
        Assert.That(plano.TemFeature(Features.RelatoriosAvancados), Is.True);
        Assert.That(plano.TemFeature(Features.AutomacoesIlimitadas), Is.True);
        Assert.That(plano.TemFeature(Features.AnexosIlimitados), Is.True);
    }

    [Test]
    public void TemFeature_FeatureVazia_RetornaFalse()
    {
        var plano = PlanoComFeatures("""{"ia":true}""");
        Assert.That(plano.TemFeature(""), Is.False);
        Assert.That(plano.TemFeature("   "), Is.False);
    }

    // --------------------------------------------------
    // ObterLimiteProfissionais / ObterLimitePacientes
    // --------------------------------------------------

    [Test]
    public void ObterLimiteProfissionais_LimitesVazio_RetornaNulo_Ilimitado()
    {
        var plano = PlanoComLimites("{}");
        Assert.That(plano.ObterLimiteProfissionais(), Is.Null);
    }

    [Test]
    public void ObterLimitePacientes_LimitesVazio_RetornaNulo_Ilimitado()
    {
        var plano = PlanoComLimites("{}");
        Assert.That(plano.ObterLimitePacientes(), Is.Null);
    }

    [Test]
    public void ObterLimiteProfissionais_ComValor_RetornaValor()
    {
        var plano = PlanoComLimites("""{"profissionais":5}""");
        Assert.That(plano.ObterLimiteProfissionais(), Is.EqualTo(5));
    }

    [Test]
    public void ObterLimitePacientes_ComValor_RetornaValor()
    {
        var plano = PlanoComLimites("""{"profissionais":5,"pacientes":100}""");
        Assert.That(plano.ObterLimitePacientes(), Is.EqualTo(100));
    }

    [Test]
    public void ObterLimiteProfissionais_JsonComNullExplicito_RetornaNulo()
    {
        var plano = PlanoComLimites("""{"profissionais":null,"pacientes":50}""");
        Assert.That(plano.ObterLimiteProfissionais(), Is.Null);
    }

    [Test]
    public void ObterLimitePacientes_ChaveAusente_RetornaNulo()
    {
        var plano = PlanoComLimites("""{"profissionais":3}""");
        Assert.That(plano.ObterLimitePacientes(), Is.Null);
    }

    [Test]
    public void ObterLimiteProfissionais_JsonMalformado_RetornaNulo_FailOpen()
    {
        // Fail-open em leitura de limite (ilimitado em dúvida — não bloquear por dados inválidos).
        var plano = PlanoComLimites("nao_e_json");
        Assert.That(plano.ObterLimiteProfissionais(), Is.Null);
    }

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------

    private ImedtoPlano PlanoComFeatures(string featuresJson)
        => ImedtoPlano.Criar("Plano Teste", null, null, false, "{}", _adminId, featuresJson: featuresJson);

    private ImedtoPlano PlanoComLimites(string limitesJson)
        => ImedtoPlano.Criar("Plano Teste", null, null, false, limitesJson, _adminId);
}
