using Imedto.Backend.Application.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Automacoes;

[TestFixture]
public class SalvarConfiguracaoAutomacaoCommandHandlerTests
{
    private Mock<IConfiguracaoAutomacaoRepository> _repo;
    private SalvarConfiguracaoAutomacaoCommandHandler _sut;

    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IConfiguracaoAutomacaoRepository>();
        _sut = new SalvarConfiguracaoAutomacaoCommandHandler(_repo.Object);
    }

    private SalvarConfiguracaoAutomacaoCommand Cmd() => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        LembretesHabilitados = true,
        HorasAntecedenciaLembrete = 24,
        ExpiracaoOrcamentosHabilitada = true,
        EmailRemetente = "noreply@imedto.com",
    };

    [Test]
    public async Task Handle_ConfigInexistente_CriaPadraoEAtualiza()
    {
        _repo.Setup(r => r.ObterPorEstabelecimento(EstabelecimentoId))
             .ReturnsAsync((ConfiguracaoAutomacao)null);

        await _sut.Handle(Cmd());

        _repo.Verify(r => r.Salvar(It.Is<ConfiguracaoAutomacao>(c =>
            c.EstabelecimentoId == EstabelecimentoId &&
            c.LembretesHabilitados &&
            c.EmailRemetente == "noreply@imedto.com")),
            Times.Once);
    }

    [Test]
    public async Task Handle_ConfigExistente_AtualizaInPlace()
    {
        var existente = ConfiguracaoAutomacao.PadraoParaEstabelecimento(EstabelecimentoId);
        _repo.Setup(r => r.ObterPorEstabelecimento(EstabelecimentoId)).ReturnsAsync(existente);

        await _sut.Handle(Cmd());

        Assert.That(existente.LembretesHabilitados, Is.True);
        Assert.That(existente.EmailRemetente, Is.EqualTo("noreply@imedto.com"));
        _repo.Verify(r => r.Salvar(existente), Times.Once);
    }

    [Test]
    public void Handle_HorasForaDoIntervalo_LancaBusinessExceptionDoAggregate()
    {
        _repo.Setup(r => r.ObterPorEstabelecimento(EstabelecimentoId))
             .ReturnsAsync((ConfiguracaoAutomacao)null);

        var cmd = Cmd();
        cmd.HorasAntecedenciaLembrete = 100;

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("antecedência"));
    }
}
