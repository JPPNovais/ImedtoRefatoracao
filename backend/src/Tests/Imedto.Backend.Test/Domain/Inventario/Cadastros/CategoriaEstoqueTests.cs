using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario.Cadastros;

[TestFixture]
public class CategoriaEstoqueTests
{
    private const string CorOk = "hsl(218 70% 50%)";
    private const string IconeOk = "fa-tag";

    [Test]
    public void Criar_DadosValidos_RetornaAggregateAtivo()
    {
        var c = CategoriaEstoque.Criar(1, "Anestésicos", CorOk, IconeOk);

        Assert.That(c.Nome, Is.EqualTo("Anestésicos"));
        Assert.That(c.Cor, Is.EqualTo(CorOk));
        Assert.That(c.Icone, Is.EqualTo(IconeOk));
        Assert.That(c.Ativo, Is.True);
        Assert.That(c.EstabelecimentoId, Is.EqualTo(1));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            CategoriaEstoque.Criar(1, "   ", CorOk, IconeOk));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }

    [Test]
    public void Criar_CorInvalida_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => CategoriaEstoque.Criar(1, "X", "#ff0000", IconeOk));
        Assert.Throws<BusinessException>(() => CategoriaEstoque.Criar(1, "X", "rgb(0,0,0)", IconeOk));
        Assert.Throws<BusinessException>(() => CategoriaEstoque.Criar(1, "X", "", IconeOk));
    }

    [Test]
    public void Criar_IconeSemPrefixoFa_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            CategoriaEstoque.Criar(1, "X", CorOk, "pills"));
        Assert.That(ex.Message, Does.Contain("fa-"));
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => CategoriaEstoque.Criar(0, "X", CorOk, IconeOk));
    }

    [Test]
    public void Atualizar_DadosValidos_AtualizaCampos()
    {
        var c = CategoriaEstoque.Criar(1, "Antes", CorOk, IconeOk);

        c.Atualizar("Depois", "hsl(0 70% 50%)", "fa-pills");

        Assert.That(c.Nome, Is.EqualTo("Depois"));
        Assert.That(c.Cor, Is.EqualTo("hsl(0 70% 50%)"));
        Assert.That(c.Icone, Is.EqualTo("fa-pills"));
        Assert.That(c.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void Atualizar_QuandoInativa_LancaBusinessException()
    {
        var c = CategoriaEstoque.Criar(1, "X", CorOk, IconeOk);
        c.Inativar();

        Assert.Throws<BusinessException>(() => c.Atualizar("Y", CorOk, IconeOk));
    }

    [Test]
    public void Inativar_QuandoJaInativa_LancaBusinessException()
    {
        var c = CategoriaEstoque.Criar(1, "X", CorOk, IconeOk);
        c.Inativar();

        Assert.Throws<BusinessException>(() => c.Inativar());
    }

    [Test]
    public void Reativar_RetornaAoStatusAtivo()
    {
        var c = CategoriaEstoque.Criar(1, "X", CorOk, IconeOk);
        c.Inativar();
        c.Reativar();
        Assert.That(c.Ativo, Is.True);
    }
}
