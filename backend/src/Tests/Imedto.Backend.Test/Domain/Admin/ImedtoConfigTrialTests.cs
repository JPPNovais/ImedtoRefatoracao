using Imedto.Backend.Domain.Admin;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre invariantes de ImedtoConfigTrial — singleton de config de trial global
/// (briefing 2026-06-11_003, F1 CA4).
/// </summary>
[TestFixture]
public class ImedtoConfigTrialTests
{
    private static readonly Guid _planoId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _adminId = Guid.NewGuid();

    [Test]
    public void CriarPadrao_PlanoValido_CriaComIdFixoEDefaults()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoId);

        Assert.That(config.Id, Is.EqualTo(ImedtoConfigTrial.IdFixo));
        Assert.That(config.PlanoTrialId, Is.EqualTo(_planoId));
        Assert.That(config.DuracaoTrialDias, Is.EqualTo(14));
        Assert.That(config.TrialHabilitado, Is.True);
    }

    [Test]
    public void CriarPadrao_PlanoIdVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoConfigTrial.CriarPadrao(Guid.Empty));
        Assert.That(ex!.Message, Does.Contain("PlanoTrialId"));
    }

    [Test]
    public void Atualizar_ValoresValidos_AtualizaCorretamente()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoId);
        var novoPlano = Guid.NewGuid();

        config.Atualizar(novoPlano, 30, false, _adminId);

        Assert.That(config.PlanoTrialId, Is.EqualTo(novoPlano));
        Assert.That(config.DuracaoTrialDias, Is.EqualTo(30));
        Assert.That(config.TrialHabilitado, Is.False);
        Assert.That(config.AtualizadoPorUsuarioId, Is.EqualTo(_adminId));
    }

    [Test]
    public void Atualizar_PlanoIdVazio_LancaBusinessException()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoId);

        var ex = Assert.Throws<BusinessException>(() =>
            config.Atualizar(Guid.Empty, 14, true, _adminId));
        Assert.That(ex!.Message, Does.Contain("PlanoTrialId"));
    }

    [Test]
    public void Atualizar_DuracaoZero_LancaBusinessException()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoId);

        var ex = Assert.Throws<BusinessException>(() =>
            config.Atualizar(_planoId, 0, true, _adminId));
        Assert.That(ex!.Message, Does.Contain("maior que zero"));
    }

    [Test]
    public void Atualizar_DuracaoNegativa_LancaBusinessException()
    {
        var config = ImedtoConfigTrial.CriarPadrao(_planoId);

        var ex = Assert.Throws<BusinessException>(() =>
            config.Atualizar(_planoId, -5, true, _adminId));
        Assert.That(ex!.Message, Does.Contain("maior que zero"));
    }
}
