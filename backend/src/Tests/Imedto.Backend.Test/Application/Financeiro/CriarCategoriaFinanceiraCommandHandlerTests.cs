using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class CriarCategoriaFinanceiraCommandHandlerTests
{
    private Mock<ICategoriaFinanceiraRepository> _repo;
    private CriarCategoriaFinanceiraCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ICategoriaFinanceiraRepository>();
        _sut = new CriarCategoriaFinanceiraCommandHandler(_repo.Object);
    }

    [Test]
    public async Task Handle_TipoValido_PersisteCategoria()
    {
        await _sut.Handle(new CriarCategoriaFinanceiraCommand
        {
            EstabelecimentoId = 1, Nome = "Honorarios", Tipo = "Receita",
        });

        _repo.Verify(r => r.Salvar(It.Is<CategoriaFinanceira>(c =>
            c.Tipo == TipoCategoria.Receita && c.Nome == "Honorarios")),
            Times.Once);
    }

    [Test]
    public void Handle_TipoInvalido_LancaBusinessException()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CriarCategoriaFinanceiraCommand
        {
            EstabelecimentoId = 1, Nome = "X", Tipo = "Outro",
        }));
        Assert.That(ex.Message, Does.Contain("Tipo"));
    }
}
