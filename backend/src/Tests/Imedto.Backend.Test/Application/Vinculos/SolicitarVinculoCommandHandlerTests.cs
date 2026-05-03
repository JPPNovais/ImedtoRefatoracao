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
public class SolicitarVinculoCommandHandlerTests
{
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<ISolicitacaoVinculoRepository> _solicitacaoRepo;
    private Mock<IEventBus> _eventBus;
    private SolicitarVinculoCommandHandler _sut;

    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _donoId = Guid.NewGuid();
    private const long EstabelecimentoId = 1;

    [SetUp]
    public void SetUp()
    {
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _vinculoRepo = new Mock<IVinculoRepository>();
        _solicitacaoRepo = new Mock<ISolicitacaoVinculoRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new SolicitarVinculoCommandHandler(
            _estabRepo.Object, _vinculoRepo.Object, _solicitacaoRepo.Object, _eventBus.Object);
    }

    private Estabelecimento Estab() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private SolicitarVinculoCommand Cmd(Guid? profissional = null) => new()
    {
        ProfissionalUsuarioId = profissional ?? _profissionalId,
        EstabelecimentoId = EstabelecimentoId,
        Mensagem = "Quero atender",
    };

    [Test]
    public async Task Handle_TudoValido_CriaSolicitacaoEPublicaEvento()
    {
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _vinculoRepo.Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync((VinculoProfissionalEstabelecimento)null);
        _solicitacaoRepo.Setup(r => r.ObterPendentePorProfissionalEEstab(_profissionalId, EstabelecimentoId))
                        .ReturnsAsync((SolicitacaoVinculo)null);
        _solicitacaoRepo.Setup(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()))
                        .Callback<SolicitacaoVinculo>(s =>
                            typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(s, 7L))
                        .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is SolicitacaoVinculoCriadaEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_ProfissionalEhDono_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(profissional: _donoId)));
        Assert.That(ex.Message, Does.Contain("dono"));
        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Never);
    }

    [Test]
    public void Handle_VinculoAtivoExistente_LancaBusinessException()
    {
        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1L, _donoId);
        vinculo.Aceitar(); // ativo

        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _vinculoRepo.Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(vinculo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("vínculo"));
    }

    [Test]
    public void Handle_VinculoConvidadoExistente_LancaBusinessException()
    {
        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1L, _donoId);
        // status = Convidado por padrao

        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _vinculoRepo.Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(vinculo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("convite pendente"));
    }

    [Test]
    public async Task Handle_VinculoInativoExistente_PermiteSolicitar()
    {
        var vinculo = VinculoProfissionalEstabelecimento.Convidar(
            _profissionalId, EstabelecimentoId, 1L, _donoId);
        vinculo.Aceitar();
        vinculo.Inativar();

        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _vinculoRepo.Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(vinculo);
        _solicitacaoRepo.Setup(r => r.ObterPendentePorProfissionalEEstab(_profissionalId, EstabelecimentoId))
                        .ReturnsAsync((SolicitacaoVinculo)null);
        _solicitacaoRepo.Setup(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()))
                        .Callback<SolicitacaoVinculo>(s =>
                            typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(s, 7L))
                        .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _solicitacaoRepo.Verify(r => r.Salvar(It.IsAny<SolicitacaoVinculo>()), Times.Once);
    }

    [Test]
    public void Handle_SolicitacaoPendenteExistente_LancaBusinessException()
    {
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(Estab());
        _vinculoRepo.Setup(r => r.ObterPorProfissionalEEstabelecimentoOuNulo(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync((VinculoProfissionalEstabelecimento)null);
        _solicitacaoRepo.Setup(r => r.ObterPendentePorProfissionalEEstab(_profissionalId, EstabelecimentoId))
                        .ReturnsAsync(SolicitacaoVinculo.Solicitar(_profissionalId, EstabelecimentoId, "antiga"));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("pendente"));
    }
}
