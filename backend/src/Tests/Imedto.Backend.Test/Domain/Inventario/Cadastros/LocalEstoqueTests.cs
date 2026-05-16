using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario.Cadastros;

[TestFixture]
public class LocalEstoqueTests
{
    [Test]
    public void Criar_DadosValidos_RetornaAtivo()
    {
        var l = LocalEstoque.Criar(1, "Armário 1", TipoLocalEstoque.Armario, "2º andar", "Maria");

        Assert.That(l.Nome, Is.EqualTo("Armário 1"));
        Assert.That(l.Tipo, Is.EqualTo(TipoLocalEstoque.Armario));
        Assert.That(l.AndarSetor, Is.EqualTo("2º andar"));
        Assert.That(l.Responsavel, Is.EqualTo("Maria"));
        Assert.That(l.Ativo, Is.True);
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            LocalEstoque.Criar(1, "   ", TipoLocalEstoque.Sala, null, null));
    }

    [Test]
    public void Atualizar_DadosValidos_AtualizaCampos()
    {
        var l = LocalEstoque.Criar(1, "X", TipoLocalEstoque.Gaveta, null, null);
        l.Atualizar("Y", TipoLocalEstoque.Refrigerado, "Recepção", null);

        Assert.That(l.Nome, Is.EqualTo("Y"));
        Assert.That(l.Tipo, Is.EqualTo(TipoLocalEstoque.Refrigerado));
        Assert.That(l.AndarSetor, Is.EqualTo("Recepção"));
    }

    [Test]
    public void Inativar_QuandoAtivo_DesativaECarimba()
    {
        var l = LocalEstoque.Criar(1, "X", TipoLocalEstoque.Cofre, null, null);
        l.Inativar();
        Assert.That(l.Ativo, Is.False);
        Assert.That(l.AtualizadoEm, Is.Not.Null);
    }
}
