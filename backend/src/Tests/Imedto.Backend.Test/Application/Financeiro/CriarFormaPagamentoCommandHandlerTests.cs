using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Financeiro;

[TestFixture]
public class CriarFormaPagamentoCommandHandlerTests
{
    private Mock<IFormaPagamentoRepository> _repo;
    private CriarFormaPagamentoCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IFormaPagamentoRepository>();
        _sut = new CriarFormaPagamentoCommandHandler(_repo.Object);
    }

    [Test]
    public async Task Handle_DadosValidos_PersisteForma()
    {
        await _sut.Handle(new CriarFormaPagamentoCommand
        {
            EstabelecimentoId = 1, Nome = "PIX",
        });

        _repo.Verify(r => r.Salvar(It.Is<FormaPagamento>(f =>
            f.Nome == "PIX" && !f.Padrao)),
            Times.Once);
    }

    [Test]
    public void Handle_NomeVazio_LancaBusinessExceptionDoAggregate()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CriarFormaPagamentoCommand
        {
            EstabelecimentoId = 1, Nome = " ",
        }));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }
}
