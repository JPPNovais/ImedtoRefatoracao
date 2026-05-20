using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Termos;

/// <summary>
/// Tests do reenvio de link público autenticado (cooldown 5 min, canal email/copia).
/// </summary>
[TestFixture]
public class ReenviarLinkTermoCommandHandlerTests
{
    private Mock<ITermoEmitidoRepository> _termoRepo = null!;
    private Mock<IPacienteRepository> _pacienteRepo = null!;
    private Mock<IEstabelecimentoRepository> _estabRepo = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<ITermoAuditLogger> _audit = null!;
    private ReenviarLinkTermoCommandHandler _sut = null!;

    private const long EstabId = 1;
    private const long PacienteId = 10;
    private const long TermoId = 99;
    private readonly Guid _usuarioId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _termoRepo = new Mock<ITermoEmitidoRepository>();
        _pacienteRepo = new Mock<IPacienteRepository>();
        _estabRepo = new Mock<IEstabelecimentoRepository>();
        _email = new Mock<IEmailService>();
        _audit = new Mock<ITermoAuditLogger>();
        _sut = new ReenviarLinkTermoCommandHandler(
            _termoRepo.Object, _pacienteRepo.Object, _estabRepo.Object,
            _email.Object, Options.Create(new EmailOptions { AppBaseUrl = "https://app.imedto.com" }),
            _audit.Object, NullLogger<ReenviarLinkTermoCommandHandler>.Instance);
    }

    private TermoEmitido TermoAceiteLinkPendente(DateTime? atualizadoEm = null)
    {
        var t = TermoEmitido.Emitir(PacienteId, EstabId, 5, 1, "<p>x</p>", "x",
            AssinaturaTipo.AceiteLink, Guid.NewGuid(), TimeSpan.FromDays(30));
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(t, TermoId);
        if (atualizadoEm.HasValue)
            typeof(TermoEmitido).GetProperty(nameof(TermoEmitido.AtualizadoEm))!.SetValue(t, atualizadoEm.Value);
        return t;
    }

    private Paciente PacienteComEmail(string email = "p@example.com")
    {
        var p = Paciente.Cadastrar(EstabId, "Paciente", null, null,
            GeneroPaciente.NaoInformado, null, email, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        return p;
    }

    private ReenviarLinkTermoCommand Cmd(string canal = "email") => new()
    {
        TermoEmitidoId = TermoId,
        EstabelecimentoId = EstabId,
        SolicitanteUsuarioId = _usuarioId,
        Canal = canal,
    };

    [Test]
    public void Handle_TermoNaoEncontrado_LancaGenerico()
    {
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync((TermoEmitido)null);
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Is.EqualTo("Termo não encontrado."));
    }

    [Test]
    public async Task Handle_CanalCopia_NaoEnviaEmail()
    {
        var termo = TermoAceiteLinkPendente();
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(termo);

        var cmd = Cmd("copia");
        await _sut.Handle(cmd);

        Assert.That(cmd.TokenAceite, Is.EqualTo(termo.TokenAceite));
        _email.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.RegistrarAsync(EstabId, _usuarioId, "termo-link-copiado",
            "TermoEmitido", TermoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_CanalEmail_EnviaQuandoCooldownExpirou()
    {
        var termo = TermoAceiteLinkPendente(atualizadoEm: DateTime.UtcNow.AddMinutes(-10));
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(termo);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(PacienteComEmail());

        await _sut.Handle(Cmd());

        _email.Verify(e => e.EnviarAsync("p@example.com", It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _termoRepo.Verify(r => r.Salvar(termo), Times.Once);
    }

    [Test]
    public void Handle_CanalEmailDentroCooldown_Lanca422()
    {
        var termo = TermoAceiteLinkPendente(atualizadoEm: DateTime.UtcNow.AddMinutes(-2));
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(termo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Does.Contain("Aguarde"));
        _email.Verify(e => e.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Handle_PacienteSemEmail_Lanca422()
    {
        var termo = TermoAceiteLinkPendente();
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(termo);
        var p = Paciente.Cadastrar(EstabId, "X", null, null, GeneroPaciente.NaoInformado, null, null, null, null);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteId);
        _pacienteRepo.Setup(r => r.ObterPorIdOuNulo(PacienteId, EstabId)).ReturnsAsync(p);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Does.Contain("e-mail"));
    }

    [Test]
    public void Handle_TermoNaoPendente_Lanca422()
    {
        var termo = TermoAceiteLinkPendente();
        termo.RegistrarAceitePublico("1.2.3.4", "ua");
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(termo);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd()));
        Assert.That(ex!.Message, Does.Contain("não está pendente").IgnoreCase);
    }

    [Test]
    public void Handle_CanalInvalido_Lanca422()
    {
        var termo = TermoAceiteLinkPendente();
        _termoRepo.Setup(r => r.ObterPorIdOuNulo(TermoId, EstabId)).ReturnsAsync(termo);
        var ex = Assert.ThrowsAsync<BusinessException>(() => _sut.Handle(Cmd("sms")));
        Assert.That(ex!.Message, Does.Contain("Canal inválido"));
    }
}
