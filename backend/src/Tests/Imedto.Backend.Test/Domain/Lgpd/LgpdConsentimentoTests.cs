using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Lgpd;

[TestFixture]
public class LgpdConsentimentoTests
{
    [Test]
    public void Aceitar_Valido_GravaCampos()
    {
        var uid = Guid.NewGuid();
        var c = LgpdConsentimento.Aceitar(uid, TipoConsentimentoLgpd.TermosUso, "v1.2",
            "192.168.0.1", "Mozilla");

        Assert.That(c.UsuarioId, Is.EqualTo(uid));
        Assert.That(c.Tipo, Is.EqualTo(TipoConsentimentoLgpd.TermosUso));
        Assert.That(c.Versao, Is.EqualTo("v1.2"));
        Assert.That(c.IpOrigem, Is.EqualTo("192.168.0.1"));
        Assert.That(c.UserAgent, Is.EqualTo("Mozilla"));
        Assert.That(c.AceitoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Aceitar_SemIpOuUserAgent_PermiteNull()
    {
        var c = LgpdConsentimento.Aceitar(Guid.NewGuid(), TipoConsentimentoLgpd.PoliticaPrivacidade, "v1");
        Assert.That(c.IpOrigem, Is.Null);
        Assert.That(c.UserAgent, Is.Null);
    }

    [Test]
    public void Aceitar_VersaoTrimada_RemoveEspacos()
    {
        var c = LgpdConsentimento.Aceitar(Guid.NewGuid(), TipoConsentimentoLgpd.TermosUso, " v2.0 ");
        Assert.That(c.Versao, Is.EqualTo("v2.0"));
    }

    [Test]
    public void Aceitar_UsuarioEmpty_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            LgpdConsentimento.Aceitar(Guid.Empty, TipoConsentimentoLgpd.TermosUso, "v1"));
    }

    [Test]
    public void Aceitar_VersaoVazia_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            LgpdConsentimento.Aceitar(Guid.NewGuid(), TipoConsentimentoLgpd.TermosUso, " "));
    }
}
