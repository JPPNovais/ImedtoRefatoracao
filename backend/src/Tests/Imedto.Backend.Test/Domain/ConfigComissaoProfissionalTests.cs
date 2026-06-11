using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

/// <summary>
/// Testes de domínio para ConfigComissaoProfissional (CA176–CA177).
/// </summary>
[TestFixture]
public class ConfigComissaoProfissionalTests
{
    private const long EstabelecimentoId = 1;
    private readonly Guid _profId = Guid.NewGuid();

    [Test]
    public void Criar_PercentualValido_ConfiguracaoCriada()
    {
        var config = ConfigComissaoProfissional.Criar(
            EstabelecimentoId, _profId, TipoComissao.Consulta, 25m);

        Assert.That(config.EstabelecimentoId, Is.EqualTo(EstabelecimentoId));
        Assert.That(config.ProfissionalUsuarioId, Is.EqualTo(_profId));
        Assert.That(config.Tipo, Is.EqualTo(TipoComissao.Consulta));
        Assert.That(config.Percentual, Is.EqualTo(25m));
    }

    [Test]
    public void Criar_Percentual0_Permitido()
    {
        var config = ConfigComissaoProfissional.Criar(
            EstabelecimentoId, _profId, TipoComissao.Procedimento, 0m);

        Assert.That(config.Percentual, Is.EqualTo(0m));
    }

    [Test]
    public void Criar_Percentual100_Permitido()
    {
        var config = ConfigComissaoProfissional.Criar(
            EstabelecimentoId, _profId, TipoComissao.Consulta, 100m);

        Assert.That(config.Percentual, Is.EqualTo(100m));
    }

    [TestCase(-0.01)]
    [TestCase(100.01)]
    [TestCase(-50)]
    [TestCase(200)]
    public void Criar_PercentualForaDoRange_LancaBusinessException(decimal percentual)
    {
        Assert.Throws<BusinessException>(() =>
            ConfigComissaoProfissional.Criar(
                EstabelecimentoId, _profId, TipoComissao.Consulta, percentual));
    }

    [Test]
    public void Atualizar_NovoPercentualValido_AtualizaValor()
    {
        var config = ConfigComissaoProfissional.Criar(
            EstabelecimentoId, _profId, TipoComissao.Consulta, 25m);

        config.Atualizar(40m);

        Assert.That(config.Percentual, Is.EqualTo(40m));
    }

    [Test]
    public void Atualizar_PercentualInvalido_LancaBusinessException()
    {
        var config = ConfigComissaoProfissional.Criar(
            EstabelecimentoId, _profId, TipoComissao.Consulta, 25m);

        Assert.Throws<BusinessException>(() => config.Atualizar(150m));
    }

    [Test]
    public void PercentualPadrao_DeveSerTrinta()
    {
        Assert.That(ComissaoConfig.PercentualPadrao, Is.EqualTo(30m));
    }
}
