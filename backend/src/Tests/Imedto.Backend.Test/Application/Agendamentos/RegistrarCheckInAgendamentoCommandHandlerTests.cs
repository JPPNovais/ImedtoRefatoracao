using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class RegistrarCheckInAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private Mock<ISalaRepository> _salaRepo;
    private Mock<IAgendamentoSalaAuditRepository> _auditRepo;
    private RegistrarCheckInAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long AgendamentoId = 500;

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _salaRepo = new Mock<ISalaRepository>();
        _auditRepo = new Mock<IAgendamentoSalaAuditRepository>();
        _sut = new RegistrarCheckInAgendamentoCommandHandler(_agendaRepo.Object, _salaRepo.Object, _auditRepo.Object);
    }

    private static Agendamento CriarAgendamento(long estabId = EstabelecimentoId)
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        return Agendamento.Criar(estabId, 100L, Guid.NewGuid(), Guid.NewGuid(),
            inicio, inicio.AddMinutes(30), "Consulta", null);
    }

    private static Agendamento CriarAgendamentoConfirmado(long estabId = EstabelecimentoId)
    {
        var ag = CriarAgendamento(estabId);
        ag.Confirmar();
        return ag;
    }

    [Test]
    public async Task Handle_AgendamentoConfirmado_RegistraCheckIn()
    {
        var ag = CriarAgendamentoConfirmado();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        await _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(ag.CheckInEm, Is.Not.Null);
        Assert.That(ag.Status, Is.EqualTo(AgendamentoStatus.Confirmado));
        _agendaRepo.Verify(r => r.Salvar(ag), Times.Once);
    }

    [Test]
    public async Task Handle_AgendamentoAgendado_RegistraCheckIn()
    {
        var ag = CriarAgendamento();
        Assert.That(ag.Status, Is.EqualTo(AgendamentoStatus.Agendado));
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        await _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(ag.CheckInEm, Is.Not.Null);
        _agendaRepo.Verify(r => r.Salvar(ag), Times.Once);
    }

    [Test]
    public void Handle_AgendamentoNaoEncontrado_LancaBusinessException()
    {
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId))
            .ReturnsAsync((Agendamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_AgendamentoCancelado_LancaBusinessException()
    {
        var ag = CriarAgendamento();
        ag.Cancelar("Teste de cancelamento");
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("cancelado"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_AgendamentoConcluido_LancaBusinessException()
    {
        var ag = CriarAgendamento();
        ag.Concluir();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("concluído"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_CheckInJaRealizado_LancaBusinessException()
    {
        var ag = CriarAgendamentoConfirmado();
        ag.RegistrarCheckIn();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("Check-in já foi realizado"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public async Task Handle_ComSalaValida_AlocaEPersiste()
    {
        var ag = CriarAgendamentoConfirmado();
        var sala = Sala.Criar(EstabelecimentoId, 10L, null, "Consultório 01", "");
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(7L, EstabelecimentoId)).ReturnsAsync(sala);

        await _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = 7L,
            UsuarioSolicitanteId = Guid.NewGuid(),
        });

        Assert.That(ag.CheckInEm, Is.Not.Null);
        Assert.That(ag.SalaId, Is.EqualTo(7L));
        _auditRepo.Verify(r => r.Registrar(It.IsAny<AgendamentoSalaAudit>()), Times.Once);
    }

    [Test]
    public void Handle_ComSalaDeOutroTenant_LancaBusinessException()
    {
        var ag = CriarAgendamentoConfirmado();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(7L, EstabelecimentoId)).ReturnsAsync((Sala?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = 7L,
        }));
        Assert.That(ex.Message, Does.Contain("Sala não encontrada"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public async Task Handle_SemSala_NaoAlteraSalaAtual()
    {
        var ag = CriarAgendamentoConfirmado();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        await _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(ag.SalaId, Is.Null);
        _auditRepo.Verify(r => r.Registrar(It.IsAny<AgendamentoSalaAudit>()), Times.Never);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId))
            .ReturnsAsync((Agendamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }
}
