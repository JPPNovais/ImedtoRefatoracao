using Imedto.Backend.Application.Cirurgias.Commands;
using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Cirurgias;

[TestFixture]
public class PlanejarProcedimentoCommandHandlerTests
{
    private Mock<IProcedimentoCirurgicoRepository> _repo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IProntuarioRepository> _prontuarioRepo;
    private Mock<IAgendamentoRepository> _agendaRepo;
    private Mock<IProntuarioAcessoLogService> _acessoLog;
    private PlanejarProcedimentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long PacienteId = 100;
    private const long ProntuarioId = 200;
    private const long AgendamentoId = 300;
    private readonly Guid _solicitanteId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IProcedimentoCirurgicoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _prontuarioRepo = new Mock<IProntuarioRepository>();
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _acessoLog = new Mock<IProntuarioAcessoLogService>();
        _sut = new PlanejarProcedimentoCommandHandler(
            _repo.Object, _pacienteRepo.Object, _prontuarioRepo.Object,
            _agendaRepo.Object, _acessoLog.Object);
    }

    private static Paciente PacienteAtivo() =>
        Paciente.Cadastrar(EstabelecimentoId, "P", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);

    private static Prontuario ProntuarioDoEstab(long estabId, long pacienteId)
    {
        var p = Prontuario.Iniciar(pacienteId, estabId, 1L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, ProntuarioId);
        return p;
    }

    private static Agendamento AgendamentoNoEstab(long estabId, long pacienteId)
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        var ag = Agendamento.Criar(estabId, pacienteId, Guid.NewGuid(), Guid.NewGuid(),
            inicio, inicio.AddHours(1), "Cirurgia", null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(ag, AgendamentoId);
        return ag;
    }

    private PlanejarProcedimentoCommand Cmd(long? agendamentoId = null) => new()
    {
        EstabelecimentoId = EstabelecimentoId,
        PacienteId = PacienteId,
        ProntuarioId = ProntuarioId,
        AgendamentoId = agendamentoId,
        CirurgiaPrincipal = "Cirurgia X",
        CirurgiaCodigo = "001",
        DataAgendada = DateTime.UtcNow.AddDays(7),
        SolicitanteUsuarioId = _solicitanteId,
        EquipeInicial = new() { new(Guid.NewGuid(), "Cirurgiao") },
    };

    [Test]
    public async Task Handle_TudoValido_PersisteEAudita()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorIdOuNulo(ProntuarioId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioDoEstab(EstabelecimentoId, PacienteId));
        _repo.Setup(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()))
             .Callback<ProcedimentoCirurgico>(p =>
                 typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, 999L))
             .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.ProcedimentoIdCriado, Is.EqualTo(999L));
        _acessoLog.Verify(a => a.RegistrarAsync(
            ProntuarioId, _solicitanteId, EstabelecimentoId, TipoAcessoProntuario.Escrita), Times.Once);
    }

    [Test]
    public void Handle_PacienteCrossTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId)).ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _repo.Verify(r => r.Salvar(It.IsAny<ProcedimentoCirurgico>()), Times.Never);
    }

    [Test]
    public void Handle_ProntuarioDeOutroEstab_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _prontuarioRepo.Setup(r => r.ObterPorIdOuNulo(ProntuarioId, EstabelecimentoId))
                       .ReturnsAsync((Prontuario?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Prontuário não encontrado."));
    }

    [Test]
    public void Handle_AgendamentoDeOutroEstab_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorIdOuNulo(ProntuarioId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioDoEstab(EstabelecimentoId, PacienteId));
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId))
                   .ReturnsAsync((Agendamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd(agendamentoId: AgendamentoId)));
        Assert.That(ex.Message, Is.EqualTo("Agendamento não encontrado."));
    }

    [Test]
    public void Handle_PapelInvalidoNaEquipe_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _prontuarioRepo.Setup(r => r.ObterPorIdOuNulo(ProntuarioId, EstabelecimentoId))
                       .ReturnsAsync(ProntuarioDoEstab(EstabelecimentoId, PacienteId));

        var cmd = Cmd();
        cmd.EquipeInicial = new() { new(Guid.NewGuid(), "Hacker") };

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("Papel"));
    }
}
