using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain;

[TestFixture]
public class ProntuarioTests
{
    [Test]
    public void Iniciar_Valido_CriaComCamposCorretos()
    {
        var p = Prontuario.Iniciar(pacienteId: 1, estabelecimentoId: 2, modeloDeProntuarioId: 3);
        Assert.That(p.PacienteId, Is.EqualTo(1));
        Assert.That(p.EstabelecimentoId, Is.EqualTo(2));
        Assert.That(p.ModeloDeProntuarioId, Is.EqualTo(3));
    }

    [Test]
    public void Iniciar_PacienteZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Prontuario.Iniciar(0, 1, 1));
        Assert.That(ex.Message, Does.Contain("Paciente"));
    }

    [Test]
    public void Iniciar_EstabelecimentoZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Prontuario.Iniciar(1, 0, 1));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }

    [Test]
    public void Iniciar_ModeloZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Prontuario.Iniciar(1, 1, 0));
        Assert.That(ex.Message, Does.Contain("Modelo de prontuário"));
    }

    [Test]
    public void TrocarModelo_ModeloValido_AtualizaModeloDeProntuarioId()
    {
        var p = Prontuario.Iniciar(1, 1, 1);
        p.TrocarModelo(99);
        Assert.That(p.ModeloDeProntuarioId, Is.EqualTo(99));
    }

    [Test]
    public void TrocarModelo_ModeloZero_LancaBusinessException()
    {
        var p = Prontuario.Iniciar(1, 1, 1);
        var ex = Assert.Throws<BusinessException>(() => p.TrocarModelo(0));
        Assert.That(ex.Message, Does.Contain("Modelo de prontuário"));
    }
}
