using Imedto.Backend.Infrastructure.Termos;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Termos;

[TestFixture]
public class SimpleTermoTextoExtractorTests
{
    private readonly SimpleTermoTextoExtractor _sut = new();

    [Test]
    public void Extrair_HtmlSimples_RetornaTextoLimpo()
    {
        var t = _sut.Extrair("<p>Olá <strong>mundo</strong></p>");
        Assert.That(t, Is.EqualTo("Olá mundo"));
    }

    [Test]
    public void Extrair_VariosBlocos_SeparaComQuebraDupla()
    {
        var t = _sut.Extrair("<p>Linha 1</p><p>Linha 2</p>");
        Assert.That(t, Does.Contain("Linha 1"));
        Assert.That(t, Does.Contain("Linha 2"));
    }

    [Test]
    public void Extrair_ListaDeItens_PrefixaCadaItemComHifen()
    {
        var t = _sut.Extrair("<ul><li>A</li><li>B</li></ul>");
        Assert.That(t, Does.Contain("- A"));
        Assert.That(t, Does.Contain("- B"));
    }

    [Test]
    public void Extrair_Br_QuebraSimples()
    {
        var t = _sut.Extrair("<p>L1<br>L2</p>");
        Assert.That(t.Replace("\r\n", "\n").Trim(), Is.EqualTo("L1\nL2"));
    }

    [Test]
    public void Extrair_Anchor_ViraTextoComUrlEntreParenteses()
    {
        var t = _sut.Extrair("<p>Veja <a href=\"https://imedto.com\">aqui</a></p>");
        Assert.That(t, Does.Contain("aqui (https://imedto.com)"));
    }

    [Test]
    public void Extrair_EntidadesHtml_Decodificadas()
    {
        var t = _sut.Extrair("<p>5 &gt; 3 &amp; 1 &lt; 2</p>");
        Assert.That(t, Does.Contain("5 > 3 & 1 < 2"));
    }

    [Test]
    public void Extrair_StringVazia_RetornaVazio()
    {
        Assert.That(_sut.Extrair(""), Is.EqualTo(string.Empty));
        Assert.That(_sut.Extrair(null), Is.EqualTo(string.Empty));
    }
}
