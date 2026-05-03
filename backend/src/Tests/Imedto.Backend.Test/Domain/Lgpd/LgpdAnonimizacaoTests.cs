using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Lgpd;

[TestFixture]
public class LgpdAnonimizacaoTests
{
    [Test]
    public void Registrar_Valido_NormalizaTabelaELowercase()
    {
        var quem = Guid.NewGuid();
        var a = LgpdAnonimizacao.Registrar(" PACIENTES ", 42L, MotivoAnonimizacao.DireitoEsquecimento, quem);

        Assert.That(a.Tabela, Is.EqualTo("pacientes"));
        Assert.That(a.RegistroId, Is.EqualTo(42L));
        Assert.That(a.Motivo, Is.EqualTo(MotivoAnonimizacao.DireitoEsquecimento));
        Assert.That(a.ExecutadoPorUsuarioId, Is.EqualTo(quem));
        Assert.That(a.AnonimizadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Registrar_SemUsuario_PermiteJobAutomatico()
    {
        var a = LgpdAnonimizacao.Registrar("usuarios", 1L, MotivoAnonimizacao.RetencaoVencida, null);
        Assert.That(a.ExecutadoPorUsuarioId, Is.Null);
    }

    [Test]
    public void Registrar_TabelaVazia_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            LgpdAnonimizacao.Registrar(" ", 1L, MotivoAnonimizacao.DireitoEsquecimento, null));
        Assert.That(ex.Message, Does.Contain("Tabela"));
    }

    [Test]
    public void Registrar_RegistroIdZero_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            LgpdAnonimizacao.Registrar("pacientes", 0L, MotivoAnonimizacao.DireitoEsquecimento, null));
        Assert.That(ex.Message, Does.Contain("Identificador"));
    }
}
