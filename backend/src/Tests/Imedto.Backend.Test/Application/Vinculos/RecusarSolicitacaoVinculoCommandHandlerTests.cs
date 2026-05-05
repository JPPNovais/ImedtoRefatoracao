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
public class RecusarSolicitacaoVinculoCommandHandlerTests
{
    private Mock<ISolicitacaoVinculoRepository> _solicitacaoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IEventBus> _eventBus;
    private RecusarSolicitacaoVinculoCommandHandler _sut;

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
        _sut = new RecusarSolicitacaoVinculoCommandHandler(
            _solicitacaoRepo.Object, _estabRepo.Object, _eventBus.Object);
    }

    private SolicitacaoVinculo SolicitacaoNoEstab(long estabId) =>
        SolicitacaoVinculo.Solicitar(_profissionalId, estabId, "Solicitacao");

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private RecusarSolicitacaoVinculoCommand Cmd(Guid? recusador = null) => new()
    {
        SolicitacaoId = SolicitacaoId,
        EstabelecimentoId = EstabelecimentoId,
        RecusadoPorUsuarioId = recusador ?? _donoId,
        Motivo = "Sem vagas",
    };

    [Test]
    public async Task Handle_DonoRecusa_TransicionaParaRecusadaEPublicaEvento()
    {
        _solicitacaoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(SolicitacaoId, EstabelecimentoId))
                        .ReturnsAsync(SolicitacaoNoEstab(EstabelecimentoId));
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        await _sut.Handle(Cmd());

        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is SolicitacaoVinculoRecusadaEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_SolicitacaoInexistente_LancaBusinessException()
    {
        _solicitacaoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(SolicitacaoId, EstabelecimentoId))
                        .ReturnsAsync((SolicitacaoVinculo?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public void Handle_CrossTenant_LancaMensagemGenericaSemConsultarEstab()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _solicitacaoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(SolicitacaoId, EstabelecimentoId))
                        .ReturnsAsync((SolicitacaoVinculo?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Solicitação não encontrada."));
        _estabRepo.Verify(r => r.ObterPorId(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void Handle_NaoEhDono_LancaBusinessException()
    {
        _solicitacaoRepo.Setup(r => r.ObterPorIdNoEstabelecimentoOuNulo(SolicitacaoId, EstabelecimentoId))
                        .ReturnsAsync(SolicitacaoNoEstab(EstabelecimentoId));
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(recusador: _outroUsuarioId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Never);
    }
}
