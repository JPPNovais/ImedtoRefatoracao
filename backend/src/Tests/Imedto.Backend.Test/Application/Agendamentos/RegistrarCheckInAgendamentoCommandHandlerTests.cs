using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class RegistrarCheckInAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private RegistrarCheckInAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long AgendamentoId = 500;

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _sut = new RegistrarCheckInAgendamentoCommandHandler(_agendaRepo.Object);
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
