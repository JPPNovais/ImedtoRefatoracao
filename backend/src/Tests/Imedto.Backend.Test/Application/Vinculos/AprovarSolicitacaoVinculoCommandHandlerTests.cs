using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Vinculos;

[TestFixture]
public class AprovarSolicitacaoVinculoCommandHandlerTests
{
    private Mock<ISolicitacaoVinculoRepository> _solicitacaoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IEventBus> _eventBus;
    private AprovarSolicitacaoVinculoCommandHandler _sut;

    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _outroUsuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long SolicitacaoId = 99;

    [SetUp]
    public void SetUp()
    {
        _solicitacaoRepo = new Mock<ISolicitacaoVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new AprovarSolicitacaoVinculoCommandHandler(
            _solicitacaoRepo.Object, _estabRepo.Object, _eventBus.Object);
    }

    private SolicitacaoVinculo SolicitacaoNoEstab(long estabId) =>
        SolicitacaoVinculo.Solicitar(_profissionalId, estabId, "Quero atender");

    private Estabelecimento EstabDoDono() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private AprovarSolicitacaoVinculoCommand Cmd(Guid? aprovador = null) => new()
    {
        SolicitacaoId = SolicitacaoId,
        EstabelecimentoId = EstabelecimentoId,
        AprovadoPorUsuarioId = aprovador ?? _donoId,
    };

    [Test]
    public async Task Handle_DonoAprovaSolicitacaoDoSeuEstab_AprovaEPublicaEvento()
    {
        var s = SolicitacaoNoEstab(EstabelecimentoId);
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId)).ReturnsAsync(s);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(EstabDoDono());

        await _sut.Handle(Cmd());

        Assert.That(s.Status, Is.EqualTo(StatusSolicitacaoVinculo.Aprovada));
        Assert.That(s.RespondidaPorUsuarioId, Is.EqualTo(_donoId));
        _solicitacaoRepo.Verify(r => r.Salvar(s), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is SolicitacaoVinculoAprovadaEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_SolicitacaoInexistente_LancaBusinessException()
    {
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId)).ReturnsAsync((SolicitacaoVinculo)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public void Handle_SolicitacaoDeOutroTenant_LancaMensagemGenericaENaoVazaExistencia()
    {
        var s = SolicitacaoNoEstab(OutroEstabId); // solicitacao pertence a OUTRO estab
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId)).ReturnsAsync(s);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Solicitação não encontrada."),
            "Cross-tenant: mesma mensagem que 'inexistente' — defense-in-depth contra enumeration.");
        _estabRepo.Verify(r => r.ObterPorId(It.IsAny<long>()), Times.Never,
            "Curto-circuito antes de bater no repositorio de estabelecimento.");
    }

    [Test]
    public void Handle_AprovadoPorNaoEhDono_LancaBusinessException()
    {
        var s = SolicitacaoNoEstab(EstabelecimentoId);
        _solicitacaoRepo.Setup(r => r.ObterPorIdOuNulo(SolicitacaoId)).ReturnsAsync(s);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(EstabDoDono());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(aprovador: _outroUsuarioId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Never);
    }
}
