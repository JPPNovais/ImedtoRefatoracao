using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Inventario.Cadastros;

[TestFixture]
public class FornecedorEstoqueTests
{
    private const string CnpjValido = "11222333000181";

    private static FornecedorEstoque Criar(string? cnpj = CnpjValido, string tipoPrazo = "corridos") =>
        FornecedorEstoque.Criar(
            estabelecimentoId: 1,
            razaoSocial: "ACME LTDA",
            nomeFantasia: "ACME",
            cnpj: cnpj,
            contatoNome: "João",
            contatoTelefone: "(11) 99999-9999",
            contatoEmail: "contato@acme.com",
            prazoEntregaDias: 7,
            tipoPrazoEntrega: tipoPrazo);

    [Test]
    public void Criar_DadosValidos_RetornaAtivo()
    {
        var f = Criar();

        Assert.That(f.RazaoSocial, Is.EqualTo("ACME LTDA"));
        Assert.That(f.Cnpj, Is.EqualTo(CnpjValido));
        Assert.That(f.ContatoEmail, Is.EqualTo("contato@acme.com"));
        Assert.That(f.PrazoEntregaDias, Is.EqualTo(7));
        Assert.That(f.TipoPrazoEntrega, Is.EqualTo("corridos"));
        Assert.That(f.Ativo, Is.True);
    }

    [Test]
    public void Criar_TipoPrazoUteis_ArmazenaUteis()
    {
        var f = Criar(tipoPrazo: "uteis");
        Assert.That(f.TipoPrazoEntrega, Is.EqualTo("uteis"));
    }

    [Test]
    public void Criar_TipoPrazoInvalido_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => Criar(tipoPrazo: "semanal"));
        Assert.Throws<BusinessException>(() => Criar(tipoPrazo: ""));
        Assert.Throws<BusinessException>(() => Criar(tipoPrazo: "Corridos")); // maiúscula inválida
    }

    [Test]
    public void Atualizar_TipoPrazo_AlteraValor()
    {
        var f = Criar(); // corridos por padrão
        Assert.That(f.TipoPrazoEntrega, Is.EqualTo("corridos"));

        f.Atualizar("ACME LTDA", null, null, null, null, null, 7, "uteis");
        Assert.That(f.TipoPrazoEntrega, Is.EqualTo("uteis"));
    }

    [Test]
    public void Atualizar_TipoPrazoInvalido_LancaBusinessException()
    {
        var f = Criar();
        Assert.Throws<BusinessException>(() => f.Atualizar("ACME LTDA", null, null, null, null, null, 7, "mensal"));
    }

    [Test]
    public void Criar_SemCnpj_ArmazenaNull()
    {
        var f = Criar(cnpj: null);
        Assert.That(f.Cnpj, Is.Null);

        var f2 = Criar(cnpj: "   ");
        Assert.That(f2.Cnpj, Is.Null);
    }

    [Test]
    public void Criar_CnpjComFormatacao_NormalizaParaDigitos()
    {
        var f = Criar(cnpj: "11.222.333/0001-81");
        Assert.That(f.Cnpj, Is.EqualTo(CnpjValido));
    }

    [Test]
    public void Criar_CnpjInvalido_LancaBusinessException()
    {
        // DV errado
        Assert.Throws<BusinessException>(() => Criar(cnpj: "11222333000100"));
        // 14 dígitos iguais
        Assert.Throws<BusinessException>(() => Criar(cnpj: "11111111111111"));
        // Comprimento errado
        Assert.Throws<BusinessException>(() => Criar(cnpj: "123"));
    }

    [Test]
    public void Criar_EmailInvalido_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() => FornecedorEstoque.Criar(
            1, "X", null, null, null, null, "isso nao eh email", 5));
        Assert.That(ex.Message, Does.Contain("E-mail"));
    }

    [Test]
    public void Criar_PrazoNegativo_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => FornecedorEstoque.Criar(
            1, "X", null, null, null, null, null, -1));
    }

    [Test]
    public void Criar_RazaoSocialVazia_LancaBusinessException()
    {
        Assert.Throws<BusinessException>(() => FornecedorEstoque.Criar(
            1, "  ", null, null, null, null, null, 5));
    }

    [Test]
    public void Atualizar_QuandoInativo_LancaBusinessException()
    {
        var f = Criar();
        f.Inativar();
        Assert.Throws<BusinessException>(() => f.Atualizar("Y", null, null, null, null, null, 5));
    }
}
