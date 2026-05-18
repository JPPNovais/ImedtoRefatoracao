using Imedto.Backend.Application.Atestados.Commands;
using Imedto.Backend.Contracts.Atestados.Commands;
using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.Domain.Atestados.Events;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Atestados;

[TestFixture]
public class EmitirAtestadoCommandHandlerTests
{
    private Mock<IAtestadoRepository> _atestadoRepo = null!;
    private Mock<IPacienteRepository> _pacienteRepo = null!;
    private Mock<IProntuarioRepository> _prontuarioRepo = null!;
    private Mock<IProntuarioAcessoLogService> _acessoLog = null!;
    private Mock<IEventBus> _eventBus = null!;
    private EmitirAtestadoCommandHandler _sut = null!;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private readonly Guid _profissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _atestadoRepo = new Mock<IAtestadoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _eventBus = new Mock<IEventBus>();
        _sut = new EmitirAtestadoCommandHandler(
            _atestadoRepo.Object, _pacienteRepo.Object, _prontuarioRepo.Object,
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

    private EmitirAtestadoCommand Cmd() => new()
    {
        PacienteId = PacienteId,
        EstabelecimentoId = EstabelecimentoId,
        ProfissionalUsuarioId = _profissionalId,
        Tipo = "Afastamento",
        DiasAfastamento = 3,
        Cid10 = "J06.9",
        Conteudo = "Atestado para afastamento por gripe.",
    };

    [Test]
    public async Task Handle_TudoValido_EmitePersisteEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorPaciente(PacienteId, EstabelecimentoId))
            .ReturnsAsync(ProntuarioJaIniciado());
        _atestadoRepo.Setup(r => r.Salvar(It.IsAny<Atestado>()))
            .Callback<Atestado>(a => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(a, 999L))
            .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.AtestadoIdCriado, Is.EqualTo(999L));
        _acessoLog.Verify(a => a.RegistrarAsync(
            ProntuarioId, _profissionalId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is AtestadoEmitidoEvent)), Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Is.EqualTo("Paciente não encontrado."));
    }

    [Test]
    public void Handle_AfastamentoSemDias_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync(PacienteAtivo());
        var cmd = Cmd();
        cmd.DiasAfastamento = null;
        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
    }
}
