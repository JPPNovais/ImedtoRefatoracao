using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class CriarModeloDeProntuarioCommandHandlerTests
{
    private Mock<IModeloDeProntuarioRepository> _repo;
    private CriarModeloDeProntuarioCommandHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IModeloDeProntuarioRepository>();
        _sut = new CriarModeloDeProntuarioCommandHandler(_repo.Object);
    }

    [Test]
    public async Task Handle_DadosValidos_PersisteModelo()
    {
        await _sut.Handle(new CriarModeloDeProntuarioCommand
        {
            EstabelecimentoId = 1,
            Nome = "Cardiologia",
            Descricao = "Modelo padrão",
            EstruturaJson = "{\"campos\":[]}",
        });

        _repo.Verify(r => r.Salvar(It.Is<ModeloDeProntuario>(m =>
            m.EstabelecimentoId == 1 && m.Nome == "Cardiologia" && !m.EhPadraoSistema)),
            Times.Once);
    }

    [Test]
    public void Handle_NomeVazio_LancaBusinessExceptionDoAggregate()
    {
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CriarModeloDeProntuarioCommand
        {
            EstabelecimentoId = 1,
            Nome = " ",
            EstruturaJson = "{}",
        }));
        Assert.That(ex.Message, Does.Contain("Nome"));
    }
}
