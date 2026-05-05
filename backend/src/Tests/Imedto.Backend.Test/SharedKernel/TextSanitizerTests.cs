using Imedto.Backend.SharedKernel.Text;
using NUnit.Framework;

namespace Imedto.Backend.Test.SharedKernel;

[TestFixture]
public class TextSanitizerTests
{
    // --- SomenteDigitos ---

    [TestCase("(11) 99999-8888", "11999998888")]
    [TestCase("123.456.789-09", "12345678909")]
    [TestCase("abc 12 def 34", "1234")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void SomenteDigitos_RemoveTudoQueNaoEhDigito(string entrada, string esperado)
    {
        Assert.That(TextSanitizer.SomenteDigitos(entrada), Is.EqualTo(esperado));
    }

    // --- TrimOuNulo ---

    [TestCase("  Joao  ", "Joao")]
    [TestCase("Joao", "Joao")]
    public void TrimOuNulo_StringComConteudo_RetornaTrim(string entrada, string esperado)
    {
        Assert.That(TextSanitizer.TrimOuNulo(entrada), Is.EqualTo(esperado));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void TrimOuNulo_StringVaziaOuSoEspaco_RetornaNull(string entrada)
    {
        Assert.That(TextSanitizer.TrimOuNulo(entrada), Is.Null);
    }

    // --- DigitosOuNulo ---

    [TestCase("(11) 99999-8888", "11999998888")]
    [TestCase("123.456.789-09", "12345678909")]
    public void DigitosOuNulo_ComDigitos_RetornaApenasDigitos(string entrada, string esperado)
    {
        Assert.That(TextSanitizer.DigitosOuNulo(entrada), Is.EqualTo(esperado));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("abc def")] // sem digito
    public void DigitosOuNulo_SemDigitos_RetornaNull(string entrada)
    {
        Assert.That(TextSanitizer.DigitosOuNulo(entrada), Is.Null);
    }
}
