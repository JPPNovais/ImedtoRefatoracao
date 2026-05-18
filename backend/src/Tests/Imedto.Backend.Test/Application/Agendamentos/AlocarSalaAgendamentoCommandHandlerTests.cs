using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class AlocarSalaAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private Mock<ISalaRepository> _salaRepo;
    private Mock<IAgendamentoSalaAuditRepository> _auditRepo;
    private AlocarSalaAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long AgendamentoId = 500;
    private const long SalaId = 7;

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _salaRepo = new Mock<ISalaRepository>();
        _auditRepo = new Mock<IAgendamentoSalaAuditRepository>();
        _sut = new AlocarSalaAgendamentoCommandHandler(_agendaRepo.Object, _salaRepo.Object, _auditRepo.Object);
    }

    private static Agendamento CriarAgendamento()
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        return Agendamento.Criar(EstabelecimentoId, 100L, Guid.NewGuid(), Guid.NewGuid(),
            inicio, inicio.AddMinutes(30), "Consulta", null);
    }

    [Test]
    public async Task Handle_SalaValida_AlocaERegistraAudit()
    {
        var ag = CriarAgendamento();
        var sala = Sala.Criar(EstabelecimentoId, 10L, null, "Consultório 01", "");
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(sala);

        await _sut.Handle(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = SalaId,
            UsuarioSolicitanteId = Guid.NewGuid(),
        });

        Assert.That(ag.SalaId, Is.EqualTo(SalaId));
        _agendaRepo.Verify(r => r.Salvar(ag), Times.Once);
        _auditRepo.Verify(r => r.Registrar(It.IsAny<AgendamentoSalaAudit>()), Times.Once);
    }

    [Test]
    public async Task Handle_SalaIdNulo_Desaloca()
    {
        var ag = CriarAgendamento();
        var salaAntes = Sala.Criar(EstabelecimentoId, 10L, null, "Consultório 01", "");
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(salaAntes);
        ag.AlocarSala(SalaId);

        await _sut.Handle(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = null,
            UsuarioSolicitanteId = Guid.NewGuid(),
        });

        Assert.That(ag.SalaId, Is.Null);
        _auditRepo.Verify(r => r.Registrar(It.IsAny<AgendamentoSalaAudit>()), Times.Once);
    }

    [Test]
    public void Handle_SalaDeOutroTenant_LancaBusinessException()
    {
        var ag = CriarAgendamento();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync((Sala?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = SalaId,
        }));
        Assert.That(ex.Message, Does.Contain("Sala não encontrada"));
        _agendaRepo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
    }

    [Test]
    public void Handle_SalaInativa_LancaBusinessException()
    {
        var ag = CriarAgendamento();
        var sala = Sala.Criar(EstabelecimentoId, 10L, null, "Consultório 01", "");
        sala.Desativar();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(sala);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = SalaId,
        }));
        Assert.That(ex.Message, Does.Contain("inativa"));
    }

    [Test]
    public void Handle_AgendamentoDeOutroTenant_LancaBusinessException()
    {
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync((Agendamento?)null);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = SalaId,
        }));
    }

    [Test]
    public async Task Handle_MesmaSalaJaAlocada_NaoGeraAudit()
    {
        var ag = CriarAgendamento();
        var sala = Sala.Criar(EstabelecimentoId, 10L, null, "Consultório 01", "");
        ag.AlocarSala(SalaId);
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        _salaRepo.Setup(r => r.ObterPorIdOuNulo(SalaId, EstabelecimentoId)).ReturnsAsync(sala);

        await _sut.Handle(new AlocarSalaAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            SalaId = SalaId,
        });

        _auditRepo.Verify(r => r.Registrar(It.IsAny<AgendamentoSalaAudit>()), Times.Never);
    }
}
