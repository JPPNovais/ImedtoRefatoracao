using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class CancelarAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private Mock<IEventBus> _eventBus;
    private CancelarAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long AgendamentoId = 500;

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _eventBus = new Mock<IEventBus>();
        _sut = new CancelarAgendamentoCommandHandler(_agendaRepo.Object, _eventBus.Object);
    }

    private static Agendamento CriarAgendamentoNoEstab(long estabId)
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        return Agendamento.Criar(estabId, 100L, Guid.NewGuid(), Guid.NewGuid(),
            inicio, inicio.AddMinutes(30), "Consulta", null);
    }

    [Test]
    public async Task Handle_AgendamentoDoMesmoTenant_CancelaEPublicaEvento()
    {
        var ag = CriarAgendamentoNoEstab(EstabelecimentoId);
        _agendaRepo.Setup(r => r.ObterPorId(AgendamentoId)).ReturnsAsync(ag);

        await _sut.Handle(new CancelarAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            Motivo = "Paciente desistiu",
        });

        Assert.That(ag.Status, Is.EqualTo(AgendamentoStatus.Cancelado));
        _agendaRepo.Verify(r => r.Salvar(ag), Times.Once);
        _eventBus.Verify(b => b.Publish(It.Is<IDomainEvent>(e => e is AgendamentoCanceladoEvent)),
            Times.Once);
    }

    [Test]
    public void Handle_AgendamentoDeOutroTenant_LancaMensagemGenericaENaoSalva()
    {
        var ag = CriarAgendamentoNoEstab(OutroEstabId); // pertence a outro estab
        _agendaRepo.Setup(r => r.ObterPorId(AgendamentoId)).ReturnsAsync(ag);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            Motivo = "tentativa cross-tenant",
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_MotivoVazio_LancaBusinessExceptionDoAggregate()
    {
        var ag = CriarAgendamentoNoEstab(EstabelecimentoId);
        _agendaRepo.Setup(r => r.ObterPorId(AgendamentoId)).ReturnsAsync(ag);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new CancelarAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            Motivo = "  ",
        }));
        Assert.That(ex.Message, Does.Contain("Motivo"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }
}
