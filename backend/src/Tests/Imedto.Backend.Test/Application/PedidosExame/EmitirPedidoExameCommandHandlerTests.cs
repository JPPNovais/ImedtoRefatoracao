using Imedto.Backend.Application.PedidosExame.Commands;
using Imedto.Backend.Contracts.PedidosExame.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.Domain.PedidosExame.Events;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.PedidosExame;

[TestFixture]
public class EmitirPedidoExameCommandHandlerTests
{
    private Mock<IPedidoExameRepository> _pedidoRepo = null!;
    private Mock<IPacienteRepository> _pacienteRepo = null!;
    private Mock<IProntuarioRepository> _prontuarioRepo = null!;
    private Mock<IProntuarioAcessoLogService> _acessoLog = null!;
    private Mock<IEventBus> _eventBus = null!;
    private EmitirPedidoExameCommandHandler _sut = null!;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private readonly Guid _profissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _pedidoRepo = new Mock<IPedidoExameRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new EmitirPedidoExameCommandHandler(
            _pedidoRepo.Object, _pacienteRepo.Object, _prontuarioRepo.Object,
            _acessoLog.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo()
    {
        var p = Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private static Prontuario ProntuarioJaIniciado()
    {
        var p = Prontuario.Iniciar(PacienteId, EstabelecimentoId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, ProntuarioId);
        return p;
    }

    private EmitirPedidoExameCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        ProfissionalUsuarioId = _profissionalId,
        Tipo = "Laboratorial",
        Exames = new List<string> { "Hemograma completo", "Glicose em jejum" },
        IndicacaoClinica = "Investigação de anemia",
    };

    [Test]
    public async Task Handle_TudoValido_EmitePersisteEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
            .ReturnsAsync(ProntuarioJaIniciado());
        _pedidoRepo.Setup(r => r.Salvar(It.IsAny<PedidoExame>()))
            .Callback<PedidoExame>(p => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 777L))
            .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.PedidoExameIdCriado, Is.EqualTo(777L));
        _acessoLog.Verify(a => a.RegistrarAsync(
            ProntuarioId, _profissionalId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is PedidoExameEmitidoEvent)), Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Is.EqualTo("Paciente não encontrado."));
    }

    [Test]
    public void Handle_SemExames_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        var cmd = Cmd();
        cmd.Exames = new List<string>();
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }
}
