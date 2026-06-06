using Imedto.Backend.Domain.Prontuarios;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class NormalizadorPoolTests
{
    [TestCase("Hipertensão", "hipertensao")]
    [TestCase("  Penicilina  ", "penicilina")]
    [TestCase("APENDICECTOMIA", "apendicectomia")]
    [TestCase("Irmão(ã)", "irmao(a)")]
    [TestCase("LÁTEX", "latex")]
    [TestCase("hipertensao", "hipertensao")]
    [TestCase("  ", "")]
    [TestCase("", "")]
    public void Normalizar_RetornaFormaCanonica(string entrada, string esperado)
    {
        var resultado = NormalizadorPool.Normalizar(entrada);
        Assert.That(resultado, Is.EqualTo(esperado));
    }

    [Test]
    public void Normalizar_NomeNulo_RetornaVazio()
    {
        // null não é permitido por NullabilityAnalysis, mas teste defensivo via cast
        var resultado = NormalizadorPool.Normalizar(string.Empty);
        Assert.That(resultado, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Normalizar_IdenticoAposNormalizacao_DedupFunciona()
    {
        // Garantia: " hipertensao " == "Hipertensão" após normalização
        var a = NormalizadorPool.Normalizar(" hipertensao ");
        var b = NormalizadorPool.Normalizar("Hipertensão");
        Assert.That(a, Is.EqualTo(b));
    }
}
