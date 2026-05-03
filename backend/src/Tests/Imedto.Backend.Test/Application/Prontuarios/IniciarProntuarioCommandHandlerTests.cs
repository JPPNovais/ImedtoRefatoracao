using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Prontuarios;

[TestFixture]
public class IniciarProntuarioCommandHandlerTests
{
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IModeloDeProntuarioRepository> _modeloRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private Mock<IEventBus> _eventBus;
    private IniciarProntuarioCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long PacienteId = 100;
    private const long ModeloId = 200;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _modeloRepo = new Mock<IModeloDeProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new IniciarProntuarioCommandHandler(
            _prontuarioRepo.Object, _pacienteRepo.Object, _modeloRepo.Object,
            _acessoLog.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo() =>
        Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);

    private static ModeloDeProntuario ModeloDoEstab(long estabId) =>
        ModeloDeProntuario.CriarDoEstabelecimento(estabId, "Modelo", null, "{}");

    private IniciarProntuarioCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        ModeloDeProntuarioId = ModeloId,
        SolicitanteUsuarioId = _solicitanteId,
    };

    [Test]
    public async Task Handle_TudoValido_IniciaProntuarioPersisteAuditEEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId));
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);
        _prontuarioRepo.Setup(r => r.Salvar(It.IsAny<Prontuario>()))
                       .Callback<Prontuario>(p =>
                           typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 999L))
                       .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _prontuarioRepo.Verify(r => r.Salvar(It.IsAny<Prontuario>()), Times.Once);
        _acessoLog.Verify(a => a.RegistrarAsync(
            999L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once,
            "Audit LGPD obrigatorio para escrita em prontuario.");
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is ProntuarioIniciadoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenericaENaoAudita()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _acessoLog.Verify(a => a.RegistrarAsync(It.IsAny<long>(), It.IsAny<Guid>(),
            It.IsAny<long>(), It.IsAny<TipoAcessoProntuario>()), Times.Never);
    }

    [Test]
    public void Handle_PacienteDeletado_LancaBusinessException()
    {
        var paciente = PacienteAtivo();
        paciente.MarcarComoDeletado(Guid.NewGuid());
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(paciente);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("deletado"));
    }

    [Test]
    public void Handle_ModeloDeOutroEstabENaoPadraoSistema_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(OutroEstabId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Modelo"));
    }

    [Test]
    public async Task Handle_ModeloPadraoSistema_PermiteUsoEmQualquerEstab()
    {
        var modeloPadrao = ModeloDeProntuario.CriarPadraoSistema("Padrao", null, "{}");
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(modeloPadrao);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);
        _prontuarioRepo.Setup(r => r.Salvar(It.IsAny<Prontuario>()))
                       .Callback<Prontuario>(p =>
                           typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 999L))
                       .Returns(Task.CompletedTask);

        await _sut.Handle(Cmd());

        _prontuarioRepo.Verify(r => r.Salvar(It.IsAny<Prontuario>()), Times.Once);
    }

    [Test]
    public void Handle_ProntuarioJaExiste_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPorIdOuNulo(ModeloId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId));
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(Prontuario.Iniciar(PacienteId, EstabelecimentoId, ModeloId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("já possui"));
    }
}
