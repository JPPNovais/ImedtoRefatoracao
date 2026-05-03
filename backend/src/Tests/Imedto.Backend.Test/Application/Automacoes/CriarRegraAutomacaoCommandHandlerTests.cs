using Imedto.Backend.Application.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Automacoes;

[TestFixture]
public class CriarRegraAutomacaoCommandHandlerTests
{
    private Mock<IRegraAutomacaoRepository> _regraRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private CriarRegraAutomacaoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _regraRepo = new Mock<IRegraAutomacaoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new CriarRegraAutomacaoCommandHandler(_regraRepo.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private CriarRegraAutomacaoCommand Cmd(Guid? solicitante = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = solicitante ?? _donoId,
        Nome = "Lembrete 24h",
        EventoGatilho = "agendamento-criado",
        CondicoesJson = "[]",
        AcoesJson = "[{\"tipo\":\"whatsapp\"}]",
    };

    [Test]
    public async Task Handle_DonoCriaRegra_Persiste()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd());

        _regraRepo.Verify(r => r.Salvar(It.IsAny<RegraAutomacao>()), Times.Once);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _regraRepo.Verify(r => r.Salvar(It.IsAny<RegraAutomacao>()), Times.Never);
    }

    [Test]
    public void Handle_EstabelecimentoInexistente_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync((Estabelecimento)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Estabelecimento"));
    }
}
