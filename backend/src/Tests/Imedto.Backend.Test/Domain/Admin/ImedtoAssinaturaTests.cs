using Imedto.Backend.Domain.Admin;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre invariantes do aggregate ImedtoAssinatura, especialmente FecharVigencia.
/// </summary>
[TestFixture]
public class ImedtoAssinaturaTests
{
    private static readonly Guid _planoId = Guid.NewGuid();
    private static readonly Guid _adminId = Guid.NewGuid();

    [Test]
    public void Criar_EstabelecimentoIdInvalido_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAssinatura.Criar(0, _planoId, false, null, _adminId));
        Assert.That(ex!.Message, Does.Contain("EstabelecimentoId"));
    }

    [Test]
    public void Criar_PlanoIdVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAssinatura.Criar(1, Guid.Empty, false, null, _adminId));
        Assert.That(ex!.Message, Does.Contain("PlanoId"));
    }

    [Test]
    public void Criar_GratuitaSemMotivo_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAssinatura.Criar(1, _planoId, true, null, _adminId));
        Assert.That(ex!.Message, Does.Contain("Motivo"));
    }

    [Test]
    public void Criar_GratuitaMotivoCurto_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoAssinatura.Criar(1, _planoId, true, "curto", _adminId));
        Assert.That(ex!.Message, Does.Contain("Motivo"));
    }

    [Test]
    public void Criar_DadosValidos_AssinaturaVigente()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        Assert.That(assinatura.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(assinatura.FimEm, Is.Null);
        Assert.That(assinatura.EstaVigente(), Is.True);
        Assert.That(assinatura.Gratuita, Is.False);
    }

    [Test]
    public void Criar_Gratuita_ComMotivoLongo_CriaCorretamente()
    {
        var motivo = new string('a', 20);
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, true, motivo, _adminId);

        Assert.That(assinatura.Gratuita, Is.True);
        Assert.That(assinatura.Motivo, Is.EqualTo(motivo));
    }

    [Test]
    public void FecharVigencia_AssinaturaVigente_FechaComSucesso()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);

        assinatura.FecharVigencia();

        Assert.That(assinatura.FimEm, Is.Not.Null);
        Assert.That(assinatura.EstaVigente(), Is.False);
    }

    [Test]
    public void FecharVigencia_JaEncerrada_LancaBusinessException()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        assinatura.FecharVigencia();

        var ex = Assert.Throws<BusinessException>(() => assinatura.FecharVigencia());
        Assert.That(ex!.Message, Does.Contain("já foi encerrada"));
    }
}
