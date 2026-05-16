using Imedto.Backend.Application.Inventario.Cadastros.Commands;
using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Inventario.Cadastros;

[TestFixture]
public class FornecedorEstoqueHandlersTests
{
    private const long EstabA = 1;
    private const string CnpjValido = "11222333000181";

    [Test]
    public void Criar_CnpjDuplicadoNoTenant_LancaBusinessException()
    {
        var repo = new Mock<IFornecedorEstoqueRepository>();
        repo.Setup(r => r.ExisteComNomeNoEstabelecimento(It.IsAny<string>(), EstabA, null)).ReturnsAsync(false);
        repo.Setup(r => r.ExisteComCnpjNoEstabelecimento(CnpjValido, EstabA, null)).ReturnsAsync(true);
        var sut = new CriarFornecedorEstoqueCommandHandler(repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new CriarFornecedorEstoqueCommand
        {
            EstabelecimentoId = EstabA,
            RazaoSocial = "ACME",
            Cnpj = CnpjValido,
            PrazoEntregaDias = 5,
        }));
        Assert.That(ex.Message, Does.Contain("CNPJ"));
        repo.Verify(r => r.Salvar(It.IsAny<FornecedorEstoque>()), Times.Never);
    }

    [Test]
    public void Criar_CnpjFormatadoQueExiste_NormalizaAntesDeVerificar()
    {
        // Quando o usuário envia "11.222.333/0001-81", o handler tem que normalizar
        // antes de checar duplicidade — senão o check passa, o aggregate normaliza,
        // e a UNIQUE constraint do DB cai como 500 genérico.
        var repo = new Mock<IFornecedorEstoqueRepository>();
        repo.Setup(r => r.ExisteComNomeNoEstabelecimento(It.IsAny<string>(), EstabA, null)).ReturnsAsync(false);
        repo.Setup(r => r.ExisteComCnpjNoEstabelecimento(CnpjValido, EstabA, null)).ReturnsAsync(true);
        var sut = new CriarFornecedorEstoqueCommandHandler(repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(() => sut.Handle(new CriarFornecedorEstoqueCommand
        {
            EstabelecimentoId = EstabA,
            RazaoSocial = "ACME",
            Cnpj = "11.222.333/0001-81",   // formatado
            PrazoEntregaDias = 5,
        }));
        Assert.That(ex.Message, Does.Contain("CNPJ"));
    }

    [Test]
    public async Task Criar_SemCnpj_NaoChecaDuplicidadeDeCnpj()
    {
        var repo = new Mock<IFornecedorEstoqueRepository>();
        repo.Setup(r => r.ExisteComNomeNoEstabelecimento(It.IsAny<string>(), EstabA, null)).ReturnsAsync(false);
        var sut = new CriarFornecedorEstoqueCommandHandler(repo.Object);

        await sut.Handle(new CriarFornecedorEstoqueCommand
        {
            EstabelecimentoId = EstabA,
            RazaoSocial = "ACME",
            Cnpj = null,
            PrazoEntregaDias = 5,
        });

        repo.Verify(r => r.ExisteComCnpjNoEstabelecimento(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long?>()), Times.Never);
        repo.Verify(r => r.Salvar(It.IsAny<FornecedorEstoque>()), Times.Once);
    }
}
