using Imedto.Backend.Domain.Admin;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// Cobre invariantes do aggregate ImedtoPlano.
/// </summary>
[TestFixture]
public class ImedtoPlanoTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoPlano.Criar("", null, null, false, "{}", _adminId));
        Assert.That(ex!.Message, Does.Contain("Nome"));
    }

    [Test]
    public void Criar_NomeMuitoLongo_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoPlano.Criar(new string('a', 101), null, null, false, "{}", _adminId));
        Assert.That(ex!.Message, Does.Contain("100 caracteres"));
    }

    [Test]
    public void Criar_PrecoNegativo_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            ImedtoPlano.Criar("Plano X", null, -1, false, "{}", _adminId));
        Assert.That(ex!.Message, Does.Contain("negativo"));
    }

    [Test]
    public void Criar_DadosValidos_PlanoAtivoComId()
    {
        var plano = ImedtoPlano.Criar("Plano Pro", "Descrição", 9900, false, "{}", _adminId);

        Assert.That(plano.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(plano.Nome, Is.EqualTo("Plano Pro"));
        Assert.That(plano.Ativo, Is.True);
        Assert.That(plano.PrecoMensalCentavos, Is.EqualTo(9900));
    }

    [Test]
    public void Inativar_PlanoAtivo_InativaComSucesso()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId);

        plano.Inativar();

        Assert.That(plano.Ativo, Is.False);
        Assert.That(plano.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void Inativar_PlanoJaInativo_LancaBusinessException()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId);
        plano.Inativar();

        var ex = Assert.Throws<BusinessException>(() => plano.Inativar());
        Assert.That(ex!.Message, Does.Contain("já está inativo"));
    }

    [Test]
    public void Reativar_PlanoInativo_ReativaComSucesso()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId);
        plano.Inativar();

        plano.Reativar();

        Assert.That(plano.Ativo, Is.True);
    }

    [Test]
    public void Reativar_PlanoJaAtivo_LancaBusinessException()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId);

        var ex = Assert.Throws<BusinessException>(() => plano.Reativar());
        Assert.That(ex!.Message, Does.Contain("já está ativo"));
    }
}
