using Imedto.Backend.Application.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Automacoes;

[TestFixture]
public class AtualizarRegraAutomacaoCommandHandlerTests
{
    private Mock<IRegraAutomacaoRepository> _regraRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private AtualizarRegraAutomacaoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _outroId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long RegraId = 50;

    [SetUp]
    public void SetUp()
    {
        _regraRepo = new Mock<IRegraAutomacaoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _sut = new AtualizarRegraAutomacaoCommandHandler(_regraRepo.Object, _estabRepo.Object);
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private static RegraAutomacao RegraNoEstab(long estabId) =>
        RegraAutomacao.Criar(estabId, "Antiga", "agendamento-criado", "[]", "[{}]");

    private AtualizarRegraAutomacaoCommand Cmd(Guid? solicitante = null) => new()
    {
        RegraId = RegraId,
        EstabelecimentoId = EstabelecimentoId,
        SolicitanteUsuarioId = solicitante ?? _donoId,
        Nome = "Atualizada",
        EventoGatilho = "agendamento-cancelado",
        CondicoesJson = "[]",
        AcoesJson = "[{\"tipo\":\"email\"}]",
    };

    [Test]
    public async Task Handle_DonoAtualiza_Persiste()
    {
        var regra = RegraNoEstab(EstabelecimentoId);
        _regraRepo.Setup(r => r.ObterPorIdOuNulo(RegraId)).ReturnsAsync(regra);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd());

        Assert.That(regra.Nome, Is.EqualTo("Atualizada"));
        _regraRepo.Verify(r => r.Salvar(regra), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenericaSemConsultarEstab()
    {
        _regraRepo.Setup(r => r.ObterPorIdOuNulo(RegraId)).ReturnsAsync(RegraNoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Regra não encontrada."));
        _estabRepo.Verify(r => r.ObterPorIdOuNulo(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _regraRepo.Setup(r => r.ObterPorIdOuNulo(RegraId)).ReturnsAsync(RegraNoEstab(EstabelecimentoId));
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(solicitante: _outroId)));
        Assert.That(ex.Message, Does.Contain("dono"));
    }

    [Test]
    public void Handle_RegraInexistente_LancaBusinessException()
    {
        _regraRepo.Setup(r => r.ObterPorIdOuNulo(RegraId)).ReturnsAsync((RegraAutomacao)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }
}
