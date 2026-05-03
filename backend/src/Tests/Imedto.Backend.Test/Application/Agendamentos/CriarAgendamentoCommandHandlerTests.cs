using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class CriarAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IVinculoRepository> _vinculoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IEventBus> _eventBus;
    private CriarAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long PacienteId = 100;
    private readonly Guid _profissionalId = Guid.NewGuid();
    private readonly Guid _criadoPorId = Guid.NewGuid();
    private readonly Guid _donoId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _vinculoRepo = new Mock<IVinculoRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new CriarAgendamentoCommandHandler(
            _agendaRepo.Object, _pacienteRepo.Object, _vinculoRepo.Object,
            _estabRepo.Object, _eventBus.Object);
    }

    private static Paciente PacienteAtivo() =>
        Paciente.Cadastrar(EstabelecimentoId, "Paciente", null, null,
            GeneroPaciente.NaoInformado, null, null, null, null);

    private Estabelecimento EstabFuncionando() =>
        Estabelecimento.Criar(_donoId, "Clinica", null, null, null, null);

    private CriarAgendamentoCommand Cmd()
    {
        // Proxima segunda 10h local — dentro do horario padrao 8-18 e dia uteis 1-5.
        var hoje = DateTime.Today;
        var diff = ((int)DayOfWeek.Monday - (int)hoje.DayOfWeek + 7) % 7;
        var segunda = hoje.AddDays(diff == 0 ? 7 : diff).AddHours(10);
        var inicio = segunda.ToUniversalTime();
        return new CriarAgendamentoCommand
        {
            EstabelecimentoId = EstabelecimentoId,
            PacienteId = PacienteId,
            ProfissionalUsuarioId = _profissionalId,
            CriadoPorUsuarioId = _criadoPorId,
            InicioPrevisto = inicio,
            FimPrevisto = inicio.AddMinutes(30),
            TipoServico = "Consulta",
        };
    }

    [Test]
    public async Task Handle_TudoValido_CriaPersisteEPublicaEvento()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(true);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(EstabFuncionando());
        _agendaRepo.Setup(r => r.ExisteConflito(_profissionalId,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                   .ReturnsAsync(false);
        _agendaRepo.Setup(r => r.Salvar(It.IsAny<Agendamento>()))
                   .Callback<Agendamento>(a =>
                       typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(a, 500L))
                   .Returns(Task.CompletedTask);

        var cmd = Cmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.AgendamentoIdCriado, Is.EqualTo(500L));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is AgendamentoCriadoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_PacienteDeOutroTenant_LancaMensagemGenerica()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync((Paciente)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Is.EqualTo("Paciente não encontrado."));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_PacienteDeletado_LancaBusinessException()
    {
        var paciente = PacienteAtivo();
        paciente.MarcarComoDeletado(Guid.NewGuid());
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(paciente);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("inativo"));
    }

    [Test]
    public void Handle_ProfissionalSemVinculoNoEstabelecimento_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("Profissional"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_ConflitoDeHorario_LancaBusinessException()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(true);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(EstabFuncionando());
        _agendaRepo.Setup(r => r.ExisteConflito(_profissionalId,
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                   .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex.Message, Does.Contain("agendamento nesse horário"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_HorarioForaDoFuncionamento_LancaBusinessExceptionDoAggregate()
    {
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
                     .ReturnsAsync(PacienteAtivo());
        _vinculoRepo.Setup(r => r.PodeAtuarComoProfissional(_profissionalId, EstabelecimentoId))
                    .ReturnsAsync(true);
        _estabRepo.Setup(r => r.ObterPorId(EstabelecimentoId)).ReturnsAsync(EstabFuncionando());

        var cmd = Cmd();
        // Modifica para 22h local — fora do funcionamento padrao 8-18.
        cmd.InicioPrevisto = cmd.InicioPrevisto.Date.ToUniversalTime().AddHours(22);
        cmd.FimPrevisto = cmd.InicioPrevisto.AddMinutes(30);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex.Message, Does.Contain("funcionamento").Or.Contain("dia").Or.Contain("bloqueada"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }
}
