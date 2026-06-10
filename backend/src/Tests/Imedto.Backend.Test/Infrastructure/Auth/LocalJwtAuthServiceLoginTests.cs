using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.SharedKernel.Domain;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Auth;

/// <summary>
/// Cobre LoginAsync — foca na correção 4 (anti-enumeração no login).
///
/// Anti-enumeração: nenhuma mensagem de erro deve permitir distinguir
/// "conta inexistente" × "senha errada" × "e-mail não confirmado".
/// As três variantes acima retornam mensagens da mesma família, e o
/// endpoint de reenvio de confirmação sempre 204 (testado no front).
///
/// Também valida o contador de tentativas: só incrementa em senha
/// incorreta (não em conta inexistente, não em e-mail não confirmado).
/// </summary>
[TestFixture]
public class LocalJwtAuthServiceLoginTests
{
    private Mock<IAuthCredencialRepository> _credenciaisRepo;
    private Mock<IAuthRefreshTokenRepository> _refreshRepo;
    private Mock<IAuthEmailTokenRepository> _emailTokenRepo;
    private Mock<IPasswordHasher> _hasher;
    private Mock<IJwtTokenIssuer> _issuer;
    private Mock<IEmailService> _emails;
    private Mock<IUsuario2faRepository> _usuario2faRepo;
    private Mock<IUsuario2faCodigoRecuperacaoRepository> _codigoRecuperacaoRepo;
    private Mock<IUsuarioSegurancaAuditRepository> _auditRepo;
    private LocalJwtAuthService _sut;

    private static readonly Guid UsuarioId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string SenhaHash = "$2a$12$HashLoginOK...........................";

    [SetUp]
    public void SetUp()
    {
        _credenciaisRepo       = new Mock<IAuthCredencialRepository>();
        _refreshRepo           = new Mock<IAuthRefreshTokenRepository>();
        _emailTokenRepo        = new Mock<IAuthEmailTokenRepository>();
        _hasher                = new Mock<IPasswordHasher>();
        _issuer                = new Mock<IJwtTokenIssuer>();
        _emails                = new Mock<IEmailService>();
        _usuario2faRepo        = new Mock<IUsuario2faRepository>();
        _codigoRecuperacaoRepo = new Mock<IUsuario2faCodigoRecuperacaoRepository>();
        _auditRepo             = new Mock<IUsuarioSegurancaAuditRepository>();

        // Sem 2FA ativo por padrão (regressão CA4)
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(It.IsAny<Guid>()))
                       .ReturnsAsync((Usuario2fa)null);

        var emailOptions = Options.Create(new EmailOptions
        {
            From = "noreply@imedto.com",
            AppBaseUrl = "https://app.imedto.com",
        });

        _sut = new LocalJwtAuthService(
            _credenciaisRepo.Object,
            _refreshRepo.Object,
            _emailTokenRepo.Object,
            _hasher.Object,
            _issuer.Object,
            _emails.Object,
            emailOptions,
            new HttpContextAccessor(),
            NullLogger<LocalJwtAuthService>.Instance,
            _usuario2faRepo.Object,
            _codigoRecuperacaoRepo.Object,
            _auditRepo.Object,
            new EphemeralDataProtectionProvider());

        // Default: refresh issuer retorna um par valido caso o caminho feliz chegue até lá.
        _issuer.Setup(i => i.EmitirAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
               .Returns(new JwtTokenEmitido("access.jwt.token", DateTime.UtcNow.AddMinutes(15)));
        _issuer.Setup(i => i.EmitirRefreshToken())
               .Returns(new RefreshTokenEmitido("refresh.cru", "refresh.hash", DateTime.UtcNow.AddDays(7)));
    }

    private static AuthCredencial CredencialConfirmada()
    {
        var c = AuthCredencial.Criar(UsuarioId, "user@imedto.com", SenhaHash);
        c.ConfirmarEmail();
        return c;
    }

    private static AuthCredencial CredencialPendenteConfirmacao()
    {
        return AuthCredencial.Criar(UsuarioId, "user@imedto.com", SenhaHash);
        // Não chama ConfirmarEmail() — fica com EmailConfirmadoEm == null.
    }

    // ─── Anti-enumeração ──────────────────────────────────────────────────────

    [Test]
    public void Login_EmailInexistente_LancaCredenciaisInvalidas()
    {
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("naoexiste@imedto.com"))
                        .ReturnsAsync((AuthCredencial)null);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("naoexiste@imedto.com", "qualquer123"));

        Assert.That(ex!.Message, Is.EqualTo("Credenciais inválidas."));
    }

    [Test]
    public void Login_SenhaIncorreta_LancaCredenciaisInvalidas()
    {
        var credencial = CredencialConfirmada();
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("user@imedto.com")).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar("senhaErrada", SenhaHash)).Returns(false);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("user@imedto.com", "senhaErrada"));

        Assert.That(ex!.Message, Is.EqualTo("Credenciais inválidas."));
    }

    [Test]
    public void Login_EmailNaoConfirmado_LancaMesmaMensagemDeCredencialInvalida()
    {
        // Correção 4 — anti-enumeração FORTE: a mensagem é IDÊNTICA aos casos de
        // "conta inexistente" e "senha errada" → atacante não consegue distinguir
        // (antes: "Confirme seu e-mail antes de entrar." revelava conta pendente).
        var credencial = CredencialPendenteConfirmacao();
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("user@imedto.com", "senhaCerta"));

        Assert.That(ex!.Message, Is.EqualTo("Credenciais inválidas."));
        // Anti-enumeração: NÃO pode revelar que o problema é o e-mail.
        Assert.That(ex.Message, Does.Not.Contain("Confirme seu e-mail"));
        Assert.That(ex.Message, Does.Not.Contain("não confirmado"),
            "Mensagem não pode mencionar 'não confirmado' — fere anti-enumeração " +
            "(combinada com /api/auth/reenviar-confirmacao sempre 204).");
    }

    [Test]
    public void Login_EmailNaoConfirmado_MensagemEhIgualAEmailInexistente()
    {
        // Validação cruzada: a mensagem em (a) e-mail inexistente, (b) senha errada
        // e (c) e-mail pendente DEVE ser a mesma string — anti-enumeração total.
        var pendente = CredencialPendenteConfirmacao();

        // (a) E-mail inexistente
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("a@imedto.com"))
                        .ReturnsAsync((AuthCredencial)null);
        var exA = Assert.ThrowsAsync<BusinessException>(() => _sut.LoginAsync("a@imedto.com", "qualquer"));

        // (c) E-mail pendente + senha correta
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("c@imedto.com")).ReturnsAsync(pendente);
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var exC = Assert.ThrowsAsync<BusinessException>(() => _sut.LoginAsync("c@imedto.com", "qualquer"));

        Assert.That(exC!.Message, Is.EqualTo(exA!.Message),
            "Anti-enumeração: e-mail pendente e e-mail inexistente devem retornar a MESMA mensagem.");
    }

    // ─── Contador de tentativas só incrementa em senha errada ───────────────

    [Test]
    public void Login_SenhaIncorreta_IncrementaTentativasFalhas()
    {
        var credencial = CredencialConfirmada();
        Assert.That(credencial.TentativasFalhas, Is.EqualTo(0), "pré-condição");

        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("user@imedto.com")).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar("errada", SenhaHash)).Returns(false);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("user@imedto.com", "errada"));

        Assert.That(credencial.TentativasFalhas, Is.EqualTo(1));
        _credenciaisRepo.Verify(r => r.Atualizar(credencial), Times.Once);
    }

    [Test]
    public void Login_EmailInexistente_NaoIncrementaContador()
    {
        // Não tem credencial pra incrementar (mas garante que nenhum Atualizar é chamado).
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>()))
                        .ReturnsAsync((AuthCredencial)null);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("naoexiste@imedto.com", "qualquer"));

        _credenciaisRepo.Verify(r => r.Atualizar(It.IsAny<AuthCredencial>()), Times.Never);
    }

    [Test]
    public void Login_EmailNaoConfirmado_NaoIncrementaContadorDeFalhas()
    {
        // Senha estava correta — não pode ser tratada como tentativa falha
        // (não queremos bloquear conta legítima só por usuário ter esquecido de confirmar).
        var credencial = CredencialPendenteConfirmacao();
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("user@imedto.com")).ReturnsAsync(credencial);
        _hasher.Setup(h => h.Verificar("senhaCerta", SenhaHash)).Returns(true);

        Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("user@imedto.com", "senhaCerta"));

        Assert.That(credencial.TentativasFalhas, Is.EqualTo(0));
        // O código atual NÃO chama Atualizar nesse caminho (não é "falha de senha").
        _credenciaisRepo.Verify(r => r.Atualizar(It.IsAny<AuthCredencial>()), Times.Never);
    }

    // ─── Caminhos auxiliares já cobertos por outras correções ───────────────

    [Test]
    public void Login_ContaBloqueada_Lanca()
    {
        var credencial = CredencialConfirmada();
        credencial.Bloquear("teste");
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("user@imedto.com")).ReturnsAsync(credencial);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("user@imedto.com", "qualquer"));

        Assert.That(ex!.Message, Does.Contain("bloqueada"));
        _hasher.Verify(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void Login_SenhaVazia_LancaCredenciaisInvalidas()
    {
        // Não pode dar mensagem diferente de "Credenciais inválidas." — anti-enumeração
        // (atacante poderia mandar senha vazia pra testar e-mails).
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _sut.LoginAsync("user@imedto.com", ""));

        Assert.That(ex!.Message, Is.EqualTo("Credenciais inválidas."));
        _credenciaisRepo.Verify(r => r.ObterPorEmailAsync(It.IsAny<string>()), Times.Never);
    }
}
