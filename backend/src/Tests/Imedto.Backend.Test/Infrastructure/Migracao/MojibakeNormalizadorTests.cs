using Imedto.Backend.Infrastructure.Migracao;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Migracao;

/// <summary>
/// Testes do MojibakeNormalizador (CA80/CA81, R-S8, D-E1).
/// </summary>
[TestFixture]
public class MojibakeNormalizadorTests
{
    // ─── CA80 — Mojibake corrigido quando seguro ──────────────────────────────

    [Test]
    [TestCase("PlÃ¡stica", "Plástica")]
    [TestCase("OrthopÃ©dia", "Orthopédia")]
    [TestCase("CirÃ³", "Ciró")]
    public void TentarCorrigir_MojibakePtBr_Corrigido(string entrada, string esperado)
    {
        var (resultado, suspeito) = MojibakeNormalizador.TentarCorrigir(entrada);
        Assert.That(suspeito, Is.False, "Correção segura não deve marcar como suspeito.");
        Assert.That(resultado, Is.EqualTo(esperado), $"'{entrada}' deve ser corrigido para '{esperado}'.");
    }

    [Test]
    public void TentarCorrigir_MojibakeCompleto_PalavraPtBr()
    {
        // "PlÃ¡stica" = "Plástica" com UTF-8 lido como Latin-1.
        var (resultado, suspeito) = MojibakeNormalizador.TentarCorrigir("Cirurgia PlÃ¡stica");
        Assert.That(suspeito, Is.False);
        Assert.That(resultado, Is.EqualTo("Cirurgia Plástica"));
    }

    // ─── CA80 — Texto correto não é alterado ─────────────────────────────────

    [Test]
    [TestCase("Joao")]
    [TestCase("joao@teste.com")]
    [TestCase("123.456.789-00")]
    [TestCase("")]
    public void TentarCorrigir_TextoAscii_NaoAlterado(string entrada)
    {
        var (resultado, suspeito) = MojibakeNormalizador.TentarCorrigir(entrada);
        Assert.That(suspeito, Is.False, "Texto ASCII não deve ser suspeito.");
        Assert.That(resultado, Is.EqualTo(entrada), "Texto ASCII não deve ser alterado.");
    }

    [Test]
    public void TentarCorrigir_TextoPortuguesJaCorreto_NaoAlterado()
    {
        // Texto em português corretamente codificado em UTF-8.
        const string textoCorreto = "São Paulo";
        var (resultado, suspeito) = MojibakeNormalizador.TentarCorrigir(textoCorreto);
        // "São Paulo" em UTF-8 contém 'ã' (U+00E3) — pode ser detectado como suspeito
        // mas após round-trip deve resultar no mesmo texto.
        Assert.That(resultado, Is.EqualTo(textoCorreto), "Texto UTF-8 correto não deve ser corrompido (CA81).");
    }

    // ─── CA81 — Não corrompe dado ambíguo ────────────────────────────────────

    [Test]
    public void TentarCorrigir_StringVazia_RetornaVazia()
    {
        var (resultado, suspeito) = MojibakeNormalizador.TentarCorrigir(string.Empty);
        Assert.That(resultado, Is.EqualTo(string.Empty));
        Assert.That(suspeito, Is.False);
    }

    [Test]
    public void NormalizarLinha_ComMojibakeEmAlgumCampo_CampoCorrigido()
    {
        var linha = new Dictionary<string, string>
        {
            ["especialidade"] = "PlÃ¡stica",
            ["nome"] = "Joao",
        };

        var (linhaCorrigida, suspeito) = MojibakeNormalizador.NormalizarLinha(linha);

        Assert.That(linhaCorrigida["especialidade"], Is.EqualTo("Plástica"),
            "Campo com mojibake deve ser corrigido.");
        Assert.That(linhaCorrigida["nome"], Is.EqualTo("Joao"),
            "Campo correto não deve ser alterado.");
        Assert.That(suspeito, Is.False, "Correção segura não marca como suspeito.");
    }
}
