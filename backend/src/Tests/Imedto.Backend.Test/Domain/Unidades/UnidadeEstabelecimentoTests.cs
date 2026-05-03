using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Unidades;

[TestFixture]
public class UnidadeEstabelecimentoTests
{
    private static EnderecoUnidadeInput EnderecoValido() =>
        new(Cep: "01310-100", Logradouro: "Av. Paulista", Numero: "1000",
            Complemento: " sala 1 ", Bairro: "Bela Vista", Cidade: "Sao Paulo", Estado: "sp");

    private static UnidadeEstabelecimento CriarValida(bool principal = true) =>
        UnidadeEstabelecimento.Criar(
            estabelecimentoId: 1L,
            nome: " Matriz ",
            isPrincipal: principal,
            endereco: EnderecoValido(),
            telefone: "(11) 99999-8888");

    // ----- Criar -----

    [Test]
    public void Criar_Valida_NormalizaCamposEDefaults()
    {
        var u = CriarValida();
        Assert.That(u.EstabelecimentoId, Is.EqualTo(1L));
        Assert.That(u.Nome, Is.EqualTo("Matriz"));
        Assert.That(u.IsPrincipal, Is.True);
        Assert.That(u.Cep, Is.EqualTo("01310100"));
        Assert.That(u.Estado, Is.EqualTo("SP"));
        Assert.That(u.Telefone, Is.EqualTo("11999998888"));
        Assert.That(u.Ativo, Is.True);
        Assert.That(u.Complemento, Is.EqualTo("sala 1"));
        Assert.That(u.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Criar_EstabelecimentoZero_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            UnidadeEstabelecimento.Criar(0L, "X", false, EnderecoValido(), null));
    }

    [Test]
    public void Criar_NomeVazio_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            UnidadeEstabelecimento.Criar(1L, " ", false, EnderecoValido(), null));
    }

    [Test]
    public void Criar_CepInvalido_LancaBusinessException()
    {
        var endereco = EnderecoValido() with { Cep = "12345" };
        var ex = Assert.Throws<BusinessException>(() =>
            UnidadeEstabelecimento.Criar(1L, "X", false, endereco, null));
        Assert.That(ex.Message, Does.Contain("CEP"));
    }

    [Test]
    public void Criar_UfTamanhoErrado_LancaBusinessException()
    {
        var endereco = EnderecoValido() with { Estado = "SAO" };
        var ex = Assert.Throws<BusinessException>(() =>
            UnidadeEstabelecimento.Criar(1L, "X", false, endereco, null));
        Assert.That(ex.Message, Does.Contain("UF"));
    }

    [Test]
    public void Criar_EnderecoTodoVazio_PermiteNull()
    {
        var endereco = new EnderecoUnidadeInput("", "", "", "", "", "", "");
        var u = UnidadeEstabelecimento.Criar(1L, "X", false, endereco, null);
        Assert.That(u.Cep, Is.Null);
        Assert.That(u.Estado, Is.Null);
        Assert.That(u.Telefone, Is.Null);
    }

    // ----- AtualizarDados -----

    [Test]
    public void AtualizarDados_Valido_AtualizaCampos()
    {
        var u = CriarValida();
        var novoEndereco = EnderecoValido() with { Cidade = "Rio de Janeiro", Estado = "rj" };
        u.AtualizarDados("Filial", novoEndereco, "21988887777");

        Assert.That(u.Nome, Is.EqualTo("Filial"));
        Assert.That(u.Cidade, Is.EqualTo("Rio de Janeiro"));
        Assert.That(u.Estado, Is.EqualTo("RJ"));
        Assert.That(u.Telefone, Is.EqualTo("21988887777"));
        Assert.That(u.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AtualizarDados_NomeVazio_LancaBusinessException()
    {
        var u = CriarValida();
        Assert.Throws<BusinessException>(() => u.AtualizarDados(" ", EnderecoValido(), null));
    }

    // ----- MarcarComoPrincipal / RemoverFlagPrincipal -----

    [Test]
    public void MarcarComoPrincipal_DeNaoPrincipal_AtivaFlag()
    {
        var u = CriarValida(principal: false);
        u.MarcarComoPrincipal();
        Assert.That(u.IsPrincipal, Is.True);
        Assert.That(u.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void MarcarComoPrincipal_JaPrincipal_NaoAtualiza()
    {
        var u = CriarValida(principal: true);
        var antes = u.AtualizadoEm;
        u.MarcarComoPrincipal();
        Assert.That(u.AtualizadoEm, Is.EqualTo(antes), "Sem mudança não deve marcar como atualizado.");
    }

    [Test]
    public void RemoverFlagPrincipal_Principal_RemoveFlag()
    {
        var u = CriarValida(principal: true);
        u.RemoverFlagPrincipal();
        Assert.That(u.IsPrincipal, Is.False);
        Assert.That(u.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void RemoverFlagPrincipal_NaoPrincipal_NaoAtualiza()
    {
        var u = CriarValida(principal: false);
        var antes = u.AtualizadoEm;
        u.RemoverFlagPrincipal();
        Assert.That(u.AtualizadoEm, Is.EqualTo(antes));
    }
}
