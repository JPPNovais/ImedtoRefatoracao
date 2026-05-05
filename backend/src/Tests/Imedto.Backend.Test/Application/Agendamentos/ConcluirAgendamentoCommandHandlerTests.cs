using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class ConcluirAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private ConcluirAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long OutroEstabId = 2;
    private const long AgendamentoId = 500;

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _sut = new ConcluirAgendamentoCommandHandler(_agendaRepo.Object);
    }

    private static Agendamento CriarAgendamentoNoEstab(long estabId)
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        return Agendamento.Criar(estabId, 100L, Guid.NewGuid(), Guid.NewGuid(),
            inicio, inicio.AddMinutes(30), "Consulta", null);
    }

    [Test]
    public async Task Handle_DoMesmoTenant_TransicionaParaConcluido()
    {
        var ag = CriarAgendamentoNoEstab(EstabelecimentoId);
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);

        await _sut.Handle(new ConcluirAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        });

        Assert.That(ag.Status, Is.EqualTo(AgendamentoStatus.Concluido));
        _agendaRepo.Verify(r => r.Salvar(ag), Times.Once);
    }

    [Test]
    public void Handle_DeOutroTenant_LancaMensagemGenerica()
    {
        // Repo filtra por tenant: chamado com EstabelecimentoId, retorna null.
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId))
            .ReturnsAsync((Agendamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new ConcluirAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
        }));
        Assert.That(ex.Message, Does.Contain("não encontrado"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }
}
