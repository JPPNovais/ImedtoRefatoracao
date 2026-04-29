using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Assinaturas;

[TestFixture]
public class AssinaturaTests
{
    [Test]
    public void IniciarTrial_ComDuracaoPadraode14Dias_StatusTrialEExpiraEmCorreto()
    {
        var antes = DateTime.UtcNow;

        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));

        Assert.That(assinatura.Status, Is.EqualTo(StatusAssinatura.Trial));
        Assert.That(assinatura.ExpiraEm, Is.Not.Null);
        Assert.That(assinatura.ExpiraEm!.Value, Is.GreaterThanOrEqualTo(antes.AddDays(14)));
    }

    [Test]
    public void IniciarTrial_DuracaoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Assinatura.IniciarTrial(1, 2, TimeSpan.Zero));

        Assert.That(ex.Message, Does.Contain("Duração do trial deve ser positiva"));
    }

    [Test]
    public void IniciarTrial_DuracaoNegativa_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(-1)));

        Assert.That(ex.Message, Does.Contain("Duração do trial deve ser positiva"));
    }

    [Test]
    public void Ativar_StatusTrial_MudaParaAtiva()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));

        assinatura.Ativar();

        Assert.That(assinatura.Status, Is.EqualTo(StatusAssinatura.Ativa));
    }

    [Test]
    public void Ativar_StatusAtiva_LancaBusinessException()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        assinatura.Ativar();

        var ex = Assert.Throws<BusinessException>(() => assinatura.Ativar());

        Assert.That(ex.Message, Does.Contain("trial podem ser ativadas"));
    }

    [Test]
    public void Expirar_TrialNaoVencido_MudaParaExpirada()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));

        assinatura.Expirar();

        Assert.That(assinatura.Status, Is.EqualTo(StatusAssinatura.Expirada));
    }

    [Test]
    public void Expirar_JaExpirada_Idempotente()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        assinatura.Expirar();

        Assert.DoesNotThrow(() => assinatura.Expirar());
        Assert.That(assinatura.Status, Is.EqualTo(StatusAssinatura.Expirada));
    }

    [Test]
    public void Expirar_StatusAtiva_LancaBusinessException()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        assinatura.Ativar();

        var ex = Assert.Throws<BusinessException>(() => assinatura.Expirar());

        Assert.That(ex.Message, Does.Contain("trial podem expirar"));
    }

    [Test]
    public void EstaAtiva_TrialDentroDoPrazo_RetornaTrue()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));

        Assert.That(assinatura.EstaAtiva(DateTime.UtcNow), Is.True);
    }

    [Test]
    public void EstaAtiva_TrialExpirado_RetornaFalse()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        var referenciaFutura = DateTime.UtcNow.AddDays(30);

        Assert.That(assinatura.EstaAtiva(referenciaFutura), Is.False);
    }

    [Test]
    public void EstaAtiva_StatusAtiva_RetornaTrue()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        assinatura.Ativar();

        Assert.That(assinatura.EstaAtiva(DateTime.UtcNow), Is.True);
    }

    [Test]
    public void EstaAtiva_StatusCancelada_RetornaFalse()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        assinatura.Cancelar();

        Assert.That(assinatura.EstaAtiva(DateTime.UtcNow), Is.False);
    }

    [Test]
    public void Cancelar_JaCancelada_LancaBusinessException()
    {
        var assinatura = Assinatura.IniciarTrial(1, 2, TimeSpan.FromDays(14));
        assinatura.Cancelar();

        var ex = Assert.Throws<BusinessException>(() => assinatura.Cancelar());

        Assert.That(ex.Message, Does.Contain("já está cancelada"));
    }

    [Test]
    public void IniciarTrial_EstabelecimentoInvalido_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Assinatura.IniciarTrial(0, 2, TimeSpan.FromDays(14)));

        Assert.That(ex.Message, Does.Contain("Estabelecimento inválido"));
    }
}
