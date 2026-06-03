using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Imedto.Backend.Application.Agendamentos.Events;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure.Auth;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

/// <summary>
/// Testes do handler de evento de reagendamento (R8, R9):
/// degradação graciosa sem e-mail + falha de envio não bloqueia.
/// </summary>
[TestFixture]
public class EnviarEmailAgendamentoReagendadoEventHandlerTests
{
    private Mock<IAgendamentoRepository> _agendamentoRepo;
    private Mock<IPacienteRepository> _pacienteRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IUsuarioRepository> _usuarioRepo;
    private Mock<IEmailService> _email;
    private EnviarEmailAgendamentoReagendadoEventHandler _sut;

    private const long EstabelecimentoId = 1;
    private const long AgendamentoId = 42;
    private const long PacienteId = 99;
    private static readonly Guid ProfissionalId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _agendamentoRepo = new Mock<IAgendamentoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _usuarioRepo = new Mock<IUsuarioRepository>();
        _email = new Mock<IEmailService>();

        var emailOptions = Options.Create(new EmailOptions { AppBaseUrl = "https://app.imedto.com" });

        _sut = new EnviarEmailAgendamentoReagendadoEventHandler(
            _agendamentoRepo.Object,
            _pacienteRepo.Object,
            _estabRepo.Object,
            _usuarioRepo.Object,
            _email.Object,
            emailOptions,
            NullLogger<EnviarEmailAgendamentoReagendadoEventHandler>.Instance);
    }

    private AgendamentoReagendadoEvent CriarEvento() =>
        new AgendamentoReagendadoEvent(
            AgendamentoId,
            EstabelecimentoId,
            PacienteId,
            ProfissionalId,
            DateTime.UtcNow.AddHours(4));

    private Mock<Paciente> CriarPacienteComEmail(string email)
    {
        var mock = new Mock<Paciente>();
        mock.Setup(p => p.Email).Returns(email);
        return mock;
    }

    private Mock<Agendamento> CriarAgendamentoMock()
    {
        var mock = new Mock<Agendamento>();
        mock.Setup(a => a.TipoServico).Returns("Consulta");
        return mock;
    }

    // ── CA8: degradação graciosa sem e-mail ──────────────────────────────

    [Test]
    public async Task Handle_PacienteSemEmail_NaoEnviaEmail()
    {
        // CA8: paciente sem e-mail → pular envio, não falhar.
        var paciente = CriarPacienteComEmail(string.Empty);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
            .ReturnsAsync(paciente.Object);

        await _sut.Handle(CriarEvento());

        _email.Verify(e => e.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_PacienteNulo_NaoEnviaEmail()
    {
        // CA8: paciente não encontrado → pular envio, não falhar.
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
            .ReturnsAsync((Paciente?)null);

        await _sut.Handle(CriarEvento());

        _email.Verify(e => e.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── CA9: falha de envio não bloqueia ──────────────────────────────────

    [Test]
    public async Task Handle_SesLancaExcecao_NaoRelanca()
    {
        // CA9: exceção no envio → LogWarning, não relança.
        var paciente = CriarPacienteComEmail("paciente@teste.com");
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
            .ReturnsAsync(paciente.Object);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId))
            .ReturnsAsync((Estabelecimento?)null);
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(ProfissionalId))
            .ReturnsAsync((Usuario?)null);
        _agendamentoRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId))
            .ReturnsAsync(CriarAgendamentoMock().Object);

        _email.Setup(e => e.EnviarAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SES indisponível"));

        // Act — não deve lançar
        Assert.DoesNotThrowAsync(() => _sut.Handle(CriarEvento()));
    }

    // ── Caminho feliz: e-mail enviado ────────────────────────────────────

    [Test]
    public async Task Handle_PacienteComEmail_EnviaEmail()
    {
        // Caminho feliz: paciente com e-mail → EnviarAsync chamado uma vez.
        var paciente = CriarPacienteComEmail("paciente@teste.com");
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabelecimentoId))
            .ReturnsAsync(paciente.Object);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(EstabelecimentoId))
            .ReturnsAsync((Estabelecimento?)null);
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(ProfissionalId))
            .ReturnsAsync((Usuario?)null);
        _agendamentoRepo.Setup(r => r.ObterPorIdOuNulo(AgendamentoId, EstabelecimentoId))
            .ReturnsAsync(CriarAgendamentoMock().Object);

        await _sut.Handle(CriarEvento());

        _email.Verify(e => e.EnviarAsync(
            "paciente@teste.com",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
