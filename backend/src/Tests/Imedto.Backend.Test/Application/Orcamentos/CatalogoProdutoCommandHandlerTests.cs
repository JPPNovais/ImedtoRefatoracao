using Imedto.Backend.Application.Orcamentos.Catalogos;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Orcamentos;

[TestFixture]
public class CatalogoProdutoCommandHandlerTests
{
    private const long EstabA = 1;

    [Test]
    public void Criar_TipoVazio_LancaBusinessExceptionDeObrigatoriedade()
    {
        var repo = new Mock<ICatalogoProdutoRepository>();
        var sut = new CriarCatalogoProdutoCommandHandler(repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new CriarCatalogoProdutoCommand
            {
                EstabelecimentoId = EstabA,
                Nome = "Prótese",
                Tipo = ""
            }));

        Assert.That(ex!.Message, Is.EqualTo("Tipo do produto é obrigatório."));
        repo.Verify(r => r.Salvar(It.IsAny<CatalogoProduto>()), Times.Never);
    }

    [Test]
    public void Criar_TipoNulo_LancaBusinessExceptionDeObrigatoriedade()
    {
        var repo = new Mock<ICatalogoProdutoRepository>();
        var sut = new CriarCatalogoProdutoCommandHandler(repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new CriarCatalogoProdutoCommand
            {
                EstabelecimentoId = EstabA,
                Nome = "Prótese",
                Tipo = null
            }));

        Assert.That(ex!.Message, Is.EqualTo("Tipo do produto é obrigatório."));
        repo.Verify(r => r.Salvar(It.IsAny<CatalogoProduto>()), Times.Never);
    }

    [Test]
    public void Criar_TipoInvalido_LancaBusinessExceptionDeInvalido()
    {
        var repo = new Mock<ICatalogoProdutoRepository>();
        var sut = new CriarCatalogoProdutoCommandHandler(repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new CriarCatalogoProdutoCommand
            {
                EstabelecimentoId = EstabA,
                Nome = "Prótese",
                Tipo = "Inexistente"
            }));

        Assert.That(ex!.Message, Is.EqualTo("Tipo de produto inválido."));
        repo.Verify(r => r.Salvar(It.IsAny<CatalogoProduto>()), Times.Never);
    }

    [Test]
    public async Task Criar_TipoValido_PersisteEntidade()
    {
        var repo = new Mock<ICatalogoProdutoRepository>();
        var sut = new CriarCatalogoProdutoCommandHandler(repo.Object);

        await sut.Handle(new CriarCatalogoProdutoCommand
        {
            EstabelecimentoId = EstabA,
            Nome = "Prótese mamária",
            Tipo = "OPME",
            ValorReferencia = 1500m
        });

        repo.Verify(r => r.Salvar(It.Is<CatalogoProduto>(p =>
            p.EstabelecimentoId == EstabA &&
            p.Nome == "Prótese mamária" &&
            p.Tipo == TipoOrcamentoProduto.OPME)), Times.Once);
    }

    [Test]
    public void Atualizar_TipoVazio_LancaBusinessExceptionDeObrigatoriedade()
    {
        var repo = new Mock<ICatalogoProdutoRepository>();
        var existente = CatalogoProduto.Criar(EstabA, "Prótese", null, null, false,
            TipoOrcamentoProduto.OPME, null, null, null, null);
        repo.Setup(r => r.ObterPorIdOuNulo(10, EstabA)).ReturnsAsync(existente);

        var sut = new AtualizarCatalogoProdutoCommandHandler(repo.Object);

        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await sut.Handle(new AtualizarCatalogoProdutoCommand
            {
                Id = 10,
                EstabelecimentoId = EstabA,
                Nome = "Prótese",
                Tipo = ""
            }));

        Assert.That(ex!.Message, Is.EqualTo("Tipo do produto é obrigatório."));
    }
}
