using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Agendamentos;

/// <summary>
/// CA18 — token válido de agendamento Agendado → Confirmado (200).
/// CA19 — token inválido/expirado/cancelado → BusinessException com mensagem genérica → 410.
/// CA20 — idempotência: já Confirmado → JaConfirmado (200 sem alterar estado).
/// CA22 — log de acesso gravado em todo fluxo (visualizou / confirmou / idempotente / inválido).
/// </summary>
[TestFixture]
public class ConfirmarPresencaPublicaCommandHandlerTests
{
    private Mock<IAgendamentoRepository> _repo;
    private ConfirmarPresencaPublicaCommandHandler _sut;

    private const string TokenValido = "abcdefg1234567890abcdefg1234567890abcdefg12";

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IAgendamentoRepository>();
        _sut  = new ConfirmarPresencaPublicaCommandHandler(_repo.Object);
    }

    private Mock<Agendamento> CriarAgendamentoMock(
        AgendamentoStatus status = AgendamentoStatus.Agendado,
        string? token = TokenValido,
        DateTime? expira = null)
    {
        var mock = new Mock<Agendamento>();
        mock.Setup(a => a.Status).Returns(status);
        mock.Setup(a => a.Id).Returns(42);
        mock.Setup(a => a.EstabelecimentoId).Returns(1);
        mock.Setup(a => a.TokenConfirmacao).Returns(token);
        mock.Setup(a => a.TokenConfirmacaoExpiraEm).Returns(expira ?? DateTime.UtcNow.AddDays(1));
        return mock;
    }

    private ConfirmarPresencaPublicaCommand CriarCmd(string? token = TokenValido) =>
        new ConfirmarPresencaPublicaCommand
        {
            Token      = token ?? string.Empty,
            IpOrigem   = "1.2.3.4",
            UserAgent  = "TestAgent/1.0",
        };

    // ── CA19: token inexistente ────────────────────────────────────────────

    [Test]
    public async Task Handle_TokenNaoEncontrado_LancaBusinessException()
    {
        _repo.Setup(r => r.ObterPorTokenOuNulo(It.IsAny<string>()))
            .ReturnsAsync((Agendamento?)null);

        var cmd = CriarCmd();
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Is.EqualTo(Agendamento.MensagemLinkInvalido));
    }

    [Test]
    public async Task Handle_TokenVazio_LancaBusinessException()
    {
        var cmd = CriarCmd(string.Empty);
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(cmd));
        Assert.That(ex!.Message, Is.EqualTo(Agendamento.MensagemLinkInvalido));
    }

    // ── CA19: cancelado → 410 genérico ────────────────────────────────────

    [Test]
    public async Task Handle_AgendamentoCancelado_LancaBusinessException()
    {
        var ag = CriarAgendamentoMock(AgendamentoStatus.Cancelado);
        _repo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _repo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(CriarCmd()));
        Assert.That(ex!.Message, Is.EqualTo(Agendamento.MensagemLinkInvalido));
    }

    // ── CA20: idempotência — já Confirmado → 200 JaConfirmado ─────────────

    [Test]
    public async Task Handle_JaConfirmado_RetornaJaConfirmado_SemAlterarEstado()
    {
        var ag = CriarAgendamentoMock(AgendamentoStatus.Confirmado);
        _repo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _repo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        var cmd = CriarCmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.Resultado, Is.EqualTo(ResultadoConfirmacaoPresenca.JaConfirmado));
        // Salvar NÃO deve ser chamado (estado não muda).
        _repo.Verify(r => r.Salvar(It.IsAny<Agendamento>()), Times.Never);
        // Mas log DE acesso deve ser gravado.
        _repo.Verify(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()), Times.Once);
    }

    // ── CA18: caminho feliz — Agendado → Confirmado ───────────────────────

    [Test]
    public async Task Handle_TokenValido_AgendadoViraConfirmado()
    {
        var ag = CriarAgendamentoMock(AgendamentoStatus.Agendado);
        _repo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _repo.Setup(r => r.Salvar(It.IsAny<Agendamento>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        var cmd = CriarCmd();
        await _sut.Handle(cmd);

        Assert.That(cmd.Resultado, Is.EqualTo(ResultadoConfirmacaoPresenca.ConfirmadoAgora));
        ag.Verify(a => a.ConfirmarPorLinkPublico("1.2.3.4", "TestAgent/1.0"), Times.Once);
        _repo.Verify(r => r.Salvar(ag.Object), Times.Once);
    }

    // ── CA22: audit log gravado nos dois caminhos ─────────────────────────

    [Test]
    public async Task Handle_TokenValido_GravarAcessoLog()
    {
        var ag = CriarAgendamentoMock(AgendamentoStatus.Agendado);
        _repo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _repo.Setup(r => r.Salvar(It.IsAny<Agendamento>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        await _sut.Handle(CriarCmd());

        _repo.Verify(r => r.SalvarAcessoLog(It.Is<AgendamentoConfirmacaoAcessoLog>(
            l => l.Acao == "confirmou_presenca")),
            Times.Once);
    }

    [Test]
    public async Task Handle_JaConfirmado_GravarAcessoLogIdempotente()
    {
        var ag = CriarAgendamentoMock(AgendamentoStatus.Confirmado);
        _repo.Setup(r => r.ObterPorTokenOuNulo(TokenValido)).ReturnsAsync(ag.Object);
        _repo.Setup(r => r.SalvarAcessoLog(It.IsAny<AgendamentoConfirmacaoAcessoLog>()))
            .Returns(Task.CompletedTask);

        await _sut.Handle(CriarCmd());

        _repo.Verify(r => r.SalvarAcessoLog(It.Is<AgendamentoConfirmacaoAcessoLog>(
            l => l.Acao == "tentativa_idempotente")),
            Times.Once);
    }
}
