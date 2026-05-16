using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario.Cadastros;

[TestFixture]
public class FabricanteEstoqueTests
{
    [Test]
    public void Criar_DadosValidos_RetornaAtivo()
    {
        var f = FabricanteEstoque.Criar(1, "Pfizer", "Estados Unidos");
        Assert.That(f.Nome, Is.EqualTo("Pfizer"));
        Assert.That(f.Pais, Is.EqualTo("Estados Unidos"));
        Assert.That(f.Ativo, Is.True);
    }

    [Test]
    public void Criar_SemPais_AceitaNull()
    {
        var f = FabricanteEstoque.Criar(1, "X", null);
        Assert.That(f.Pais, Is.Null);

        var f2 = FabricanteEstoque.Criar(1, "X", "   ");
        Assert.That(f2.Pais, Is.Null);
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => FabricanteEstoque.Criar(1, " ", null));
    }

    [Test]
    public void Atualizar_QuandoInativo_LancaBusinessException()
    {
        var f = FabricanteEstoque.Criar(1, "X", null);
        f.Inativar();
        Assert.Throws<BusinessException>(() => f.Atualizar("Y", null));
    }
}
