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

    private IniciarProntuarioCommand Cmd(long? modeloId = ModeloId) => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        ModeloDeProntuarioId = modeloId,
        SolicitanteUsuarioId = _solicitanteId,
    };

    private void SetupSalvarComId(long id = 999L) =>
        _prontuarioRepo.Setup(r => r.Salvar(It.IsAny<Prontuario>()))
            .Callback<Prontuario>(p => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, id))
            .Returns(Task.CompletedTask);

    [Test]
    public async Task Handle_TudoValido_IniciaProntuarioPersisteAuditEEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId));
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);
        SetupSalvarComId();

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
        // Repo filtra padrao-sistema OR estabelecimento ativo: modelo de outro tenant retorna null.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync((ModeloDeProntuario?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Modelo"));
    }

    [Test]
    public async Task Handle_ModeloPadraoSistema_PermiteUsoEmQualquerEstab()
    {
        var modeloPadrao = ModeloDeProntuario.CriarPadraoSistema("Padrao", null, "{}");
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(modeloPadrao);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario)null);
        SetupSalvarComId();

        await _sut.Handle(Cmd());

        _prontuarioRepo.Verify(r => r.Salvar(It.IsAny<Prontuario>()), Times.Once);
    }

    [Test]
    public void Handle_ProntuarioJaExiste_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterVisivelOuNulo(ModeloId, EstabelecimentoId)).ReturnsAsync(ModeloDoEstab(EstabelecimentoId));
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
                       .ReturnsAsync(Prontuario.Iniciar(PacienteId, EstabelecimentoId, ModeloId));

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("já possui"));
    }

    // --- Novos testes: fluxo mobile (ModeloDeProntuarioId omitido) ---

    [Test]
    public async Task Handle_SemModelo_ResolveModeloPadraoDoEstabelecimento()
    {
        // Cenário: mobile envia body {} → command com ModeloDeProntuarioId = null.
        // Handler deve resolver o primeiro modelo ativo do estab e iniciar com ele.
        var modeloPadrao = ModeloDoEstab(EstabelecimentoId);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPrimeiroVisivelOuNulo(EstabelecimentoId)).ReturnsAsync(modeloPadrao);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId)).ReturnsAsync((Prontuario)null);
        SetupSalvarComId();

        await _sut.Handle(Cmd(modeloId: null));

        _prontuarioRepo.Verify(r => r.Salvar(It.IsAny<Prontuario>()), Times.Once);
        // ObterVisivelOuNulo NÃO deve ser chamado quando o caller não passou id.
        _modeloRepo.Verify(r => r.ObterVisivelOuNulo(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        _acessoLog.Verify(a => a.RegistrarAsync(999L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public async Task Handle_SemModeloENenhumModeloNoEstab_IniciaSemModelo()
    {
        // Cenário: estab sem nenhum modelo cadastrado. Mobile ainda deve conseguir iniciar
        // o prontuário (ModeloDeProntuarioId = 0 na entidade — fluxo funciona sem template).
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPrimeiroVisivelOuNulo(EstabelecimentoId)).ReturnsAsync((ModeloDeProntuario)null);
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId)).ReturnsAsync((Prontuario)null);
        SetupSalvarComId();

        await _sut.Handle(Cmd(modeloId: null));

        _prontuarioRepo.Verify(r => r.Salvar(
            It.Is<Prontuario>(p => p.ModeloDeProntuarioId == 0)), Times.Once,
            "Sem modelo disponivel, deve salvar com ModeloDeProntuarioId = 0.");
        _acessoLog.Verify(a => a.RegistrarAsync(999L, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public async Task Handle_SemModeloMultiTenant_NaoVazaModelosDeOutroEstab()
    {
        // Garante que ObterPrimeiroVisivelOuNulo é chamado com o EstabelecimentoId do tenant
        // (não com outro), mesmo quando o caller não especifica modelo.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _modeloRepo.Setup(r => r.ObterPrimeiroVisivelOuNulo(EstabelecimentoId)).ReturnsAsync((ModeloDeProntuario)null);
        _modeloRepo.Setup(r => r.ObterPrimeiroVisivelOuNulo(OutroEstabId)).ReturnsAsync(ModeloDoEstab(OutroEstabId));
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId)).ReturnsAsync((Prontuario)null);
        SetupSalvarComId();

        await _sut.Handle(Cmd(modeloId: null));

        // Deve consultar APENAS o tenant correto, nunca OutroEstabId.
        _modeloRepo.Verify(r => r.ObterPrimeiroVisivelOuNulo(EstabelecimentoId), Times.Once);
        _modeloRepo.Verify(r => r.ObterPrimeiroVisivelOuNulo(OutroEstabId), Times.Never,
            "Modelo de outro estabelecimento nao deve ser consultado.");
    }
}
