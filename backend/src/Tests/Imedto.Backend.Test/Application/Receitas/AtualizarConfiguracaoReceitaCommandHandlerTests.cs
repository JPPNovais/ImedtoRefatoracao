using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Receitas;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Receitas;

[TestFixture]
public class AtualizarConfiguracaoReceitaCommandHandlerTests
{
    private Mock<IConfiguracaoReceitaRepository> _repo;
    private AtualizarConfiguracaoReceitaCommandHandler _sut;

    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IConfiguracaoReceitaRepository>();
        _sut = new AtualizarConfiguracaoReceitaCommandHandler(_repo.Object);
    }

    private AtualizarConfiguracaoReceitaCommand Cmd() => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = Guid.NewGuid(),
        CabecalhoHtml = "<h1>Clinica</h1>",
        RodapeHtml = "<p>Endereco</p>",
        EmissorPadrao = "Dr. Joao",
    };

    [Test]
    public async Task Handle_ConfigInexistente_CriaPadraoEAtualiza()
    {
        _repo.Setup(r => r.ObterPorEstabelecimentoOuNulo(EstabelecimentoId))
             .ReturnsAsync((ConfiguracaoReceitaEstabelecimento)null);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.Is<ConfiguracaoReceitaEstabelecimento>(c =>
            c.EstabelecimentoId == EstabelecimentoId &&
            c.EmissorPadrao == "Dr. Joao")),
            Times.Once);
    }

    [Test]
    public async Task Handle_ConfigExistente_AtualizaInPlace()
    {
        var existente = ConfiguracaoReceitaEstabelecimento.CriarPadrao(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorEstabelecimentoOuNulo(EstabelecimentoId)).ReturnsAsync(existente);

        await _sut.Handle(Cmd());

        Assert.That(existente.EmissorPadrao, Is.EqualTo("Dr. Joao"));
        Assert.That(existente.CabecalhoHtml, Is.EqualTo("<h1>Clinica</h1>"));
        _repo.Verify(r => r.Salvar(existente), Times.Once);
    }
}
