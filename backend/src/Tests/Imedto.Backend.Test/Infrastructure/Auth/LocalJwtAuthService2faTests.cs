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
/// Testes de 2FA TOTP no LocalJwtAuthService.
///
/// CAs cobertos:
///  CA2  — código inválido na confirmação → 2FA não ativado
///  CA5  — login passo 1 com 2FA ativo retorna desafio, sem cookies
///  CA4  — login sem 2FA → comportamento inalterado (regressão)
///  CA7  — código de recuperação one-time → consumido, reuso falha
///  CA9  — anti-bypass: sem passo 1 não há sessão
///  CA10 — desafio expirado → erro genérico
///  CA11 — desativação caminho feliz
///  CA12 — desativação falha com só um fator
///  CA19 — auditoria gravada em ativação/desativação/uso de recuperação
///  CA21 — códigos de recuperação hasheados (nunca em claro no repositório)
/// </summary>
[TestFixture]
public class LocalJwtAuthService2faTests
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
    private IDataProtectionProvider _dp;
    private LocalJwtAuthService _sut;

    private static readonly Guid UsuarioId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string SenhaHash = "$2a$12$2faTestHashOK.............................";

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
        _dp                    = new EphemeralDataProtectionProvider();

        // defaults
        _issuer.Setup(i => i.EmitirAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
               .Returns(new JwtTokenEmitido("access.jwt", DateTime.UtcNow.AddMinutes(15)));
        _issuer.Setup(i => i.EmitirRefreshToken())
               .Returns(new RefreshTokenEmitido("refresh.cru", "refresh.hash", DateTime.UtcNow.AddDays(7)));

        _sut = CriarSut();
    }

    private LocalJwtAuthService CriarSut() => new LocalJwtAuthService(
        _credenciaisRepo.Object,
        _refreshRepo.Object,
        _emailTokenRepo.Object,
        _hasher.Object,
        _issuer.Object,
        _emails.Object,
        Options.Create(new EmailOptions { From = "noreply@imedto.com", AppBaseUrl = "https://app.imedto.com" }),
        new HttpContextAccessor(),
        NullLogger<LocalJwtAuthService>.Instance,
        _usuario2faRepo.Object,
        _codigoRecuperacaoRepo.Object,
        _auditRepo.Object,
        _dp);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private AuthCredencial CredencialAtiva()
    {
        var c = AuthCredencial.Criar(UsuarioId, "user2fa@imedto.com", SenhaHash);
        c.ConfirmarEmail();
        return c;
    }

    private void ConfigurarCredencialAtiva()
    {
        var c = CredencialAtiva();
        _credenciaisRepo.Setup(r => r.ObterPorEmailAsync("user2fa@imedto.com")).ReturnsAsync(c);
        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(c);
        _hasher.Setup(h => h.Verificar("senhaCorreta", SenhaHash)).Returns(true);
    }

    // ── CA4 — Login sem 2FA → comportamento inalterado ───────────────────────

    [Test]
    public async Task Login_Sem2FA_RetornaAuthResultNormal()
    {
        ConfigurarCredencialAtiva();
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync((Usuario2fa)null);
        _refreshRepo.Setup(r => r.AdicionarAsync(It.IsAny<AuthRefreshToken>())).Returns(Task.CompletedTask);

        var result = await _sut.LoginAsync("user2fa@imedto.com", "senhaCorreta");

        Assert.That(result, Is.Not.InstanceOf<LocalJwtAuthService.LoginResultadoComDesafio>());
        Assert.That(result.AccessToken, Is.EqualTo("access.jwt"));
    }

    // ── CA5 — Login passo 1 com 2FA ativo → retorna desafio, sem sessão ──────

    [Test]
    public async Task Login_Com2faAtivo_RetornaDesafioSemSessao()
    {
        ConfigurarCredencialAtiva();

        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));
        estado.ConfirmarAtivacao();

        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);

        var result = await _sut.LoginAsync("user2fa@imedto.com", "senhaCorreta");

        Assert.That(result, Is.InstanceOf<LocalJwtAuthService.LoginResultadoComDesafio>());
        var desafio = (LocalJwtAuthService.LoginResultadoComDesafio)result;
        Assert.That(desafio.RequerSegundoFator, Is.True);
        Assert.That(desafio.Desafio, Is.Not.Null.And.Not.Empty);
        // Sessão NÃO foi emitida (anti-bypass CA9)
        Assert.That(desafio.AccessToken, Is.Empty);
    }

    // ── CA9 — Anti-bypass: passo 2 sem desafio válido falha ──────────────────

    [Test]
    public async Task ConfirmarLogin2fa_DesafioInvalido_Lanca()
    {
        var ex = await ThrownAsync<BusinessException>(
            () => _sut.ConfirmarLogin2faAsync("token.invalido.xpto", "123456"));
        Assert.That(ex.Message, Is.EqualTo("Código inválido."));
    }

    // ── CA10 — Desafio expirado → erro genérico ───────────────────────────────

    [Test]
    public async Task ConfirmarLogin2fa_DesafioExpirado_LancaExpiracao()
    {
        // Criamos um sut com DataProtection próprio (compartilhado) mas manipulamos
        // o payload para forçar expiração
        var protector = _dp.CreateProtector("auth.totp.challenge");
        var payloadExpirado = $"{UsuarioId}:{DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds()}";
        var tokenExpirado = protector.Protect(payloadExpirado);

        var ex = await ThrownAsync<BusinessException>(
            () => _sut.ConfirmarLogin2faAsync(tokenExpirado, "123456"));
        Assert.That(ex.Message, Does.Contain("expirou").IgnoreCase);
    }

    // ── CA2 — Confirmação com código inválido → 2FA não ativado ──────────────

    [Test]
    public async Task ConfirmarAtivacao_CodigoErrado_LancaErroGenerico()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));

        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);

        // Código totalmente errado
        var valido = await _sut.ValidarCodigo2faPendenteAsync(UsuarioId, "000000");
        Assert.That(valido, Is.False);
        // 2FA permanece Pendente — não foi ativado
        Assert.That(estado.Status, Is.EqualTo(Usuario2faStatus.Pendente));
    }

    // ── CA7 — Código de recuperação one-time ─────────────────────────────────

    [Test]
    public async Task ConfirmarLogin2fa_CodigoRecuperacao_ConsomeUmaVez()
    {
        ConfigurarCredencialAtiva();

        // Monta estado 2FA ativo
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));
        estado.ConfirmarAtivacao();
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);

        // Emite desafio real
        var desafioToken = EmitirDesafioViaReflexao(UsuarioId);

        // Código de recuperação ainda disponível
        const string codigoCru = "ABCDEFGH";
        const string codigoHash = "$2a$12$RecuperacaoHash.....................";
        var codigoEntidade = Usuario2faCodigoRecuperacao.Criar(UsuarioId, codigoHash);
        _codigoRecuperacaoRepo.Setup(r => r.ListarPorUsuario(UsuarioId))
            .ReturnsAsync(new List<Usuario2faCodigoRecuperacao> { codigoEntidade });
        _hasher.Setup(h => h.Verificar(codigoCru, codigoHash)).Returns(true);
        // TOTP não bate — forçar fallback para recuperação
        _hasher.Setup(h => h.Verificar(It.Is<string>(s => s != codigoCru), SenhaHash)).Returns(false);
        _auditRepo.Setup(a => a.Adicionar(It.IsAny<UsuarioSegurancaAudit>())).Returns(Task.CompletedTask);
        _refreshRepo.Setup(r => r.AdicionarAsync(It.IsAny<AuthRefreshToken>())).Returns(Task.CompletedTask);

        var result = await _sut.ConfirmarLogin2faAsync(desafioToken, codigoCru);

        Assert.That(result.AccessToken, Is.EqualTo("access.jwt"));
        // O código foi marcado como usado (CA7)
        Assert.That(codigoEntidade.JaUsado, Is.True);
        // Auditoria gravada (CA19 parcial)
        _auditRepo.Verify(a => a.Adicionar(It.Is<UsuarioSegurancaAudit>(
            s => s.Acao == AcaoSeguranca.UsouCodigoRecuperacao)), Times.Once);
    }

    // ── CA7 variante — mesmo código reutilizado falha ─────────────────────────

    [Test]
    public async Task ConfirmarLogin2fa_CodigoRecuperacaoJaUsado_Lanca()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));
        estado.ConfirmarAtivacao();
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);

        var desafioToken = EmitirDesafioViaReflexao(UsuarioId);

        // Código JÁ usado
        const string codigoCru = "ABCDEFGH";
        const string codigoHash = "$2a$12$RecuperacaoHash.....................";
        var codigoEntidade = Usuario2faCodigoRecuperacao.Criar(UsuarioId, codigoHash);
        codigoEntidade.Consumir(); // já usado!

        _codigoRecuperacaoRepo.Setup(r => r.ListarPorUsuario(UsuarioId))
            .ReturnsAsync(new List<Usuario2faCodigoRecuperacao> { codigoEntidade });
        _hasher.Setup(h => h.Verificar(codigoCru, codigoHash)).Returns(true);

        var ex = await ThrownAsync<BusinessException>(
            () => _sut.ConfirmarLogin2faAsync(desafioToken, codigoCru));
        Assert.That(ex.Message, Is.EqualTo("Código inválido."));
    }

    // ── CA11 — Desativação caminho feliz ─────────────────────────────────────

    [Test]
    public async Task Desativar2fa_SenhaECodigoValidos_DesativaEAudita()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));
        estado.ConfirmarAtivacao();

        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(CredencialAtiva());
        _hasher.Setup(h => h.Verificar("senhaCorreta", SenhaHash)).Returns(true);
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);
        _codigoRecuperacaoRepo.Setup(r => r.ListarPorUsuario(UsuarioId))
            .ReturnsAsync(new List<Usuario2faCodigoRecuperacao>());
        _codigoRecuperacaoRepo.Setup(r => r.RemoverTodosDoUsuario(UsuarioId)).Returns(Task.CompletedTask);
        _auditRepo.Setup(a => a.Adicionar(It.IsAny<UsuarioSegurancaAudit>())).Returns(Task.CompletedTask);

        // Gera código TOTP válido para o segredo
        var codigoValido = TotpService.GerarCodigo(segredoBase32);

        await _sut.Desativar2faAsync(UsuarioId, "senhaCorreta", codigoValido);

        // 2FA removido do repositório
        _usuario2faRepo.Verify(r => r.Remover(estado), Times.Once);
        // Auditoria gravada (CA19)
        _auditRepo.Verify(a => a.Adicionar(It.Is<UsuarioSegurancaAudit>(
            s => s.Acao == AcaoSeguranca.Desativou2fa)), Times.Once);
    }

    // ── CA12 — Desativação falha com senha correta mas código errado ──────────

    [Test]
    public async Task Desativar2fa_SenhaOkCodigoErrado_MantemAtivo()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));
        estado.ConfirmarAtivacao();

        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(CredencialAtiva());
        _hasher.Setup(h => h.Verificar("senhaCorreta", SenhaHash)).Returns(true);
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);
        _codigoRecuperacaoRepo.Setup(r => r.ListarPorUsuario(UsuarioId))
            .ReturnsAsync(new List<Usuario2faCodigoRecuperacao>());

        var ex = await ThrownAsync<BusinessException>(
            () => _sut.Desativar2faAsync(UsuarioId, "senhaCorreta", "000000"));

        Assert.That(ex.Message, Does.Contain("desativar").IgnoreCase);
        // Não removeu
        _usuario2faRepo.Verify(r => r.Remover(It.IsAny<Usuario2fa>()), Times.Never);
    }

    // ── CA12 variante — código ok mas senha errada ────────────────────────────

    [Test]
    public async Task Desativar2fa_CodigoOkSenhaErrada_Lanca()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));
        estado.ConfirmarAtivacao();

        _credenciaisRepo.Setup(r => r.ObterPorIdAsync(UsuarioId)).ReturnsAsync(CredencialAtiva());
        _hasher.Setup(h => h.Verificar("senhaErrada", SenhaHash)).Returns(false);
        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);

        var codigoValido = TotpService.GerarCodigo(segredoBase32);

        var ex = await ThrownAsync<BusinessException>(
            () => _sut.Desativar2faAsync(UsuarioId, "senhaErrada", codigoValido));

        Assert.That(ex.Message, Does.Contain("desativar").IgnoreCase);
        _usuario2faRepo.Verify(r => r.Remover(It.IsAny<Usuario2fa>()), Times.Never);
    }

    // ── CA19 — Auditoria de ativação ──────────────────────────────────────────

    [Test]
    public async Task ConfirmarAtivacao_GravaAuditoriaAtivou2fa()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));

        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);
        _codigoRecuperacaoRepo.Setup(r => r.RemoverTodosDoUsuario(UsuarioId)).Returns(Task.CompletedTask);
        _codigoRecuperacaoRepo.Setup(r => r.Adicionar(It.IsAny<Usuario2faCodigoRecuperacao>())).Returns(Task.CompletedTask);
        _auditRepo.Setup(a => a.Adicionar(It.IsAny<UsuarioSegurancaAudit>())).Returns(Task.CompletedTask);
        // Hasher precisa retornar um hash válido para cada código de recuperação (não-nulo/não-vazio).
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("$2a$12$hashedCodigo..........");

        var codigos = await _sut.Confirmar2faAtivacaoAsync(UsuarioId);

        Assert.That(codigos.Count, Is.EqualTo(10)); // CA1 — exatamente 10
        _auditRepo.Verify(a => a.Adicionar(It.Is<UsuarioSegurancaAudit>(
            s => s.Acao == AcaoSeguranca.Ativou2fa && s.UsuarioId == UsuarioId)), Times.Once);
    }

    // ── CA21 — Códigos de recuperação hasheados ───────────────────────────────

    [Test]
    public async Task ConfirmarAtivacao_CodigosRecuperacaoSaoHasheados()
    {
        var segredoBase32 = TotpService.GerarSegredoBase32();
        var protector = _dp.CreateProtector("auth.totp.secret");
        var estado = Usuario2fa.IniciarAtivacao(UsuarioId, protector.Protect(segredoBase32));

        _usuario2faRepo.Setup(r => r.ObterPorUsuarioId(UsuarioId)).ReturnsAsync(estado);
        _codigoRecuperacaoRepo.Setup(r => r.RemoverTodosDoUsuario(UsuarioId)).Returns(Task.CompletedTask);
        _codigoRecuperacaoRepo.Setup(r => r.Adicionar(It.IsAny<Usuario2faCodigoRecuperacao>())).Returns(Task.CompletedTask);
        _auditRepo.Setup(a => a.Adicionar(It.IsAny<UsuarioSegurancaAudit>())).Returns(Task.CompletedTask);

        // Captura os códigos que seriam persistidos
        var entidadesGravadas = new List<Usuario2faCodigoRecuperacao>();
        _codigoRecuperacaoRepo.Setup(r => r.Adicionar(It.IsAny<Usuario2faCodigoRecuperacao>()))
            .Callback<Usuario2faCodigoRecuperacao>(c => entidadesGravadas.Add(c))
            .Returns(Task.CompletedTask);
        // Hasher mock: retorna hash fake diferente do input
        _hasher.Setup(h => h.Hash(It.IsAny<string>()))
               .Returns<string>(s => "$2a$12$FakeHash" + s[..Math.Min(s.Length, 4)]);

        var codigos = await _sut.Confirmar2faAtivacaoAsync(UsuarioId);

        Assert.That(entidadesGravadas.Count, Is.EqualTo(10));
        foreach (var (cru, entidade) in codigos.Zip(entidadesGravadas))
        {
            // Hash gravado não é o código em claro (CA21)
            Assert.That(entidade.CodigoHash, Is.Not.EqualTo(cru));
            Assert.That(entidade.CodigoHash, Does.StartWith("$2a$12$"));
        }
    }

    // ── Helpers de teste ─────────────────────────────────────────────────────

    /// <summary>
    /// Emite um desafio real usando o mesmo IDataProtector do sut.
    /// Usa reflexão para acessar o método privado (apenas em teste).
    /// </summary>
    private string EmitirDesafioViaReflexao(Guid usuarioId)
    {
        var protector = _dp.CreateProtector("auth.totp.challenge");
        var payload = $"{usuarioId}:{DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}";
        return protector.Protect(payload);
    }

    private static async Task<T> ThrownAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T ex) { return ex; }
        Assert.Fail($"Esperava {typeof(T).Name} mas não foi lançada.");
        return null!;
    }
}
