using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.Test.Helpers;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

[TestFixture]
public class RegistrarCheckInAgendamentoCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _agendaRepo;
    private Mock<ISalaRepository> _salaRepo;
    private Mock<IAgendamentoSalaAuditRepository> _auditRepo;
    private Mock<ICobrancaRepository> _cobrancaRepo;
    private Mock<IConvenioRepository> _convenioRepo;
    private RegistrarCheckInAgendamentoCommandHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long AgendamentoId = 500;

    [SetUp]
    public void SetUp()
    {
        _agendaRepo = new Mock<IAgendamentoRepository>();
        _salaRepo = new Mock<ISalaRepository>();
        _auditRepo = new Mock<IAgendamentoSalaAuditRepository>();
        _cobrancaRepo = new Mock<ICobrancaRepository>();
        _convenioRepo = new Mock<IConvenioRepository>();
        _sut = new RegistrarCheckInAgendamentoCommandHandler(
            _agendaRepo.Object, _salaRepo.Object, _auditRepo.Object, _cobrancaRepo.Object, _convenioRepo.Object);
    }

    private static Agendamento CriarAgendamento(long estabId = EstabelecimentoId)
    {
        var inicio = DateTime.UtcNow.AddDays(1);
        var ag = Agendamento.Criar(estabId, 100L, Guid.NewGuid(), Guid.NewGuid(),
            inicio, inicio.AddMinutes(30), "Consulta", null);
        // Simula Id gerado pelo banco — necessário para que CriarParaConsulta passe INV-6 (agendamentoId > 0).
        ag.SimularIdBanco(AgendamentoId);
        return ag;
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
            ValorCobrado = 150m,
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
            ValorCobrado = 150m,
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
            ValorCobrado = 150m,
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
            ValorCobrado = 150m,
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

    // ── CA1: cobrança criada no check-in ──────────────────────────────────

    [Test]
    public async Task Handle_CheckInParticular_CriaCobranca()
    {
        var ag = CriarAgendamentoConfirmado();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        Cobranca? cobrancaSalva = null;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c => cobrancaSalva = c)
            .Returns(Task.CompletedTask);

        await _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            TipoAtendimento = "Particular",
            ValorCobrado = 250m,
            UsuarioSolicitanteId = Guid.NewGuid(),
        });

        _cobrancaRepo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Once);
        Assert.That(cobrancaSalva, Is.Not.Null);
        Assert.That(cobrancaSalva!.ValorCobrado, Is.EqualTo(250m));
        Assert.That(cobrancaSalva.TipoAtendimento, Is.EqualTo(TipoAtendimento.Particular));
        Assert.That(cobrancaSalva.Status, Is.EqualTo(StatusCobranca.Aberta));
    }

    [Test]
    public async Task Handle_CheckInConvenio_CriaCobrancaConvenioSemValor()
    {
        var ag = CriarAgendamentoConfirmado();
        _agendaRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId)).ReturnsAsync(ag);
        Cobranca? cobrancaSalva = null;
        _cobrancaRepo.Setup(r => r.Salvar(It.IsAny<Cobranca>()))
            .Callback<Cobranca>(c => cobrancaSalva = c)
            .Returns(Task.CompletedTask);

        await _sut.Handle(new RegistrarCheckInAgendamentoCommand
        {
            AgendamentoId = AgendamentoId,
            EstabelecimentoId = EstabelecimentoId,
            TipoAtendimento = "Convenio",
            ValorCobrado = 0m, // R12: Convênio não tem valor no balcão
            UsuarioSolicitanteId = Guid.NewGuid(),
        });

        _cobrancaRepo.Verify(r => r.Salvar(It.IsAny<Cobranca>()), Times.Once);
        Assert.That(cobrancaSalva!.TipoAtendimento, Is.EqualTo(TipoAtendimento.Convenio));
        Assert.That(cobrancaSalva.ValorCobrado, Is.EqualTo(0m)); // R12
    }
}
