using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Termos;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Termos;

[TestFixture]
public class GanssHtmlSanitizerTests
{
    private ITermoHtmlSanitizer _sut;

    [SetUp]
    public void SetUp() => _sut = new GanssHtmlSanitizer();

    [Test]
    public void Sanitizar_TagsPermitidas_PreservadasNoOutput()
    {
        var html = "<p>Olá <strong>mundo</strong></p><ul><li>item</li></ul>";
        var saida = _sut.Sanitizar(html);
        Assert.That(saida, Does.Contain("<p>"));
        Assert.That(saida, Does.Contain("<strong>"));
        Assert.That(saida, Does.Contain("<li>"));
    }

    [Test]
    public void Sanitizar_TagScript_Removida()
    {
        var html = "<p>ok</p><script>alert('xss')</script>";
        var saida = _sut.Sanitizar(html);
        Assert.That(saida, Does.Not.Contain("<script"));
        Assert.That(saida, Does.Not.Contain("alert"));
    }

    [Test]
    public void Sanitizar_AtributoOnclick_Removido()
    {
        var html = "<p onclick=\"alert(1)\">x</p>";
        var saida = _sut.Sanitizar(html);
        Assert.That(saida, Does.Not.Contain("onclick"));
    }

    [Test]
    public void Sanitizar_Style_RemovidoEAtributoStyleRemovido()
    {
        var html = "<style>body{}</style><p style=\"color:red\">x</p>";
        var saida = _sut.Sanitizar(html);
        Assert.That(saida, Does.Not.Contain("<style"));
        Assert.That(saida, Does.Not.Contain("color"));
    }

    [Test]
    public void Sanitizar_HrefHttpsAceito_HrefJavascriptRemovido()
    {
        var saidaOk = _sut.Sanitizar("<a href=\"https://imedto.com\">link</a>");
        var saidaXss = _sut.Sanitizar("<a href=\"javascript:alert(1)\">x</a>");

        Assert.That(saidaOk, Does.Contain("href"));
        Assert.That(saidaXss, Does.Not.Contain("javascript"));
    }

    [Test]
    public void Sanitizar_Iframe_Removido()
    {
        var html = "<iframe src=\"evil.com\"></iframe>";
        Assert.That(_sut.Sanitizar(html), Does.Not.Contain("iframe"));
    }

    [Test]
    public void Sanitizar_VariaveisDePlaceholderPermanecemIntactas()
    {
        // {{paciente.nome}} é texto puro pra sanitizer — não deve mexer.
        var html = "<p>Olá {{paciente.nome}}</p>";
        Assert.That(_sut.Sanitizar(html), Does.Contain("{{paciente.nome}}"));
    }
}
