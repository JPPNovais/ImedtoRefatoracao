using Imedto.Backend.Application.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

/// <summary>
/// CA17 — payload sem PII (sem paciente_id, estabelecimento_id, CPF, nome do paciente).
/// CA19 — token inválido/expirado → BusinessException genérica.
/// CA22 — log de acesso gravado.
/// CA23 — resolve pelo token, não por tenant claim.
/// </summary>
[TestFixture]
public class ConsultarConfirmacaoPublicaQueryHandlerTests
{
    private Mock<IAgendamentoRepository> _agendamentoRepo;
    private Mock<IEstabelecimentoRepository> _estabRepo;
    private Mock<IUsuarioRepository> _usuarioRepo;
    private ConsultarConfirmacaoPublicaQueryHandler _sut;

    private const string TokenValido = "abcdefg1234567890abcdefg1234567890abcdefg12";

    [SetUp]
    public void SetUp()
    {
        _agendamentoRepo = new Mock<IAgendamentoRepository>();
        _estabRepo       = new Mock<IEstabelecimentoRepository>();
        _usuarioRepo     = new Mock<IUsuarioRepository>();
        _sut             = new ConsultarConfirmacaoPublicaQueryHandler(
            _agendamentoRepo.Object,
            _estabRepo.Object,
            _usuarioRepo.Object);
    }

    private Mock<Agendamento> CriarAgendamentoMock(
        AgendamentoStatus status = AgendamentoStatus.Agendado,
        string? token = TokenValido,
        DateTime? expira = null)
    {
        var profId = Guid.NewGuid();
        var mock = new Mock<Agendamento>();
        mock.Setup(a => a.Id).Returns(42);
        mock.Setup(a => a.EstabelecimentoId).Returns(1);
        mock.Setup(a => a.ProfissionalUsuarioId).Returns(profId);
        mock.Setup(a => a.Status).Returns(status);
        mock.Setup(a => a.TokenConfirmacao).Returns(token);
        mock.Setup(a => a.TokenConfirmacaoExpiraEm).Returns(expira ?? DateTime.UtcNow.AddDays(1));
        mock.Setup(a => a.TipoServico).Returns("Consulta");
        mock.Setup(a => a.InicioPrevisto).Returns(DateTime.UtcNow.AddDays(1));
        mock.Setup(a => a.FimPrevisto).Returns(DateTime.UtcNow.AddDays(1).AddHours(1));
        return mock;
    }

    private ConsultarConfirmacaoPublicaQuery CriarQuery(string? token = TokenValido) =>
        new ConsultarConfirmacaoPublicaQuery
        {
            Token     = token ?? string.Empty,
            IpOrigem  = "1.2.3.4",
            UserAgent = "TestAgent/1.0",
        };

    // ── CA17: payload sem PII ──────────────────────────────────────────────

    [Test]
    public async Task Handle_TokenValido_RetornaPayloadSemPii()
    {
        var ag = CriarAgendamentoMock();
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _agendamentoRepo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        var estab = new Mock<Estabelecimento>();
        estab.Setup(e => e.NomeFantasia).Returns("Clínica Teste");
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(1)).ReturnsAsync(estab.Object);

        var prof = new Mock<Usuario>();
        prof.Setup(u => u.NomeCompleto).Returns("Dr. Fulano");
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(It.IsAny<Guid>())).ReturnsAsync(prof.Object);

        var dto = await _sut.Handle(CriarQuery());

        Assert.That(dto.EstabelecimentoNome, Is.EqualTo("Clínica Teste"));
        Assert.That(dto.ProfissionalNome,    Is.EqualTo("Dr. Fulano"));
        Assert.That(dto.TipoServico,         Is.EqualTo("Consulta"));
        Assert.That(dto.InicioPrevisto,      Is.Not.Default);
    }

    // ── CA19: token inválido → BusinessException ───────────────────────────

    [Test]
    public void Handle_TokenNaoEncontrado_LancaBusinessException()
    {
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(It.IsAny<string>()))
            .ReturnsAsync((Agendamento?)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));
        Assert.That(ex!.Message, Is.EqualTo(Agendamento.MensagemLinkInvalido));
    }

    [Test]
    public void Handle_TokenExpirado_LancaBusinessException()
    {
        var ag = CriarAgendamentoMock(expira: DateTime.UtcNow.AddSeconds(-10));
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _agendamentoRepo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));
        Assert.That(ex!.Message, Is.EqualTo(Agendamento.MensagemLinkInvalido));
    }

    [Test]
    public void Handle_AgendamentoCancelado_LancaBusinessException()
    {
        var ag = CriarAgendamentoMock(AgendamentoStatus.Cancelado);
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _agendamentoRepo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));
        Assert.That(ex!.Message, Is.EqualTo(Agendamento.MensagemLinkInvalido));
    }

    // ── CA22: log de acesso gravado ────────────────────────────────────────

    [Test]
    public async Task Handle_TokenValido_GravarAcessoLogVisualizou()
    {
        var ag = CriarAgendamentoMock();
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _agendamentoRepo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(It.IsAny<long>())).ReturnsAsync((Estabelecimento?)null);
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);

        await _sut.Handle(CriarQuery());

        _agendamentoRepo.Verify(r => r.SalvarAcessoLog(It.Is<AgendamentoConfirmacaoAcessoLog>(
            l => l.Acao == "visualizou_publico")),
            Times.Once);
    }

    [Test]
    public async Task Handle_TokenExpirado_GravarAcessoLogTentativaInvalida()
    {
        var ag = CriarAgendamentoMock(expira: DateTime.UtcNow.AddSeconds(-10));
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _agendamentoRepo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarQuery()));

        _agendamentoRepo.Verify(r => r.SalvarAcessoLog(It.Is<AgendamentoConfirmacaoAcessoLog>(
            l => l.Acao == "tentativa_invalida")),
            Times.Once);
    }

    // ── CA23: resolve por token, sem tenant claim ──────────────────────────

    [Test]
    public async Task Handle_ResolveAgendamentoPeloToken_SemFiltroTenant()
    {
        // Não deve receber estabelecimentoId como parâmetro.
        var ag = CriarAgendamentoMock();
        _agendamentoRepo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _agendamentoRepo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);
        _estabRepo.Setup(r => r.ObterPorIdOuNulo(It.IsAny<long>())).ReturnsAsync((Estabelecimento?)null);
        _usuarioRepo.Setup(r => r.ObterPorIdOuNulo(It.IsAny<Guid>())).ReturnsAsync((Usuario?)null);

        await _sut.Handle(CriarQuery());

        // Confirmar que ObterPorTokenOuNulo foi chamado (não ObterPorIdOuNulo com tenant).
        _agendamentoRepo.Verify(r => r.ObterPorTokenOuNulo(TokenValido), Times.Once);
        _agendamentoRepo.Verify(r => r.ObterPorIdOuNulo(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }
}
