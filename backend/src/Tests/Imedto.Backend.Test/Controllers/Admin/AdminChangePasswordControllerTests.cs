using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Imedto.Backend.API.Controllers.Admin;
using Imedto.Backend.Contracts.Admin.Auth;
using Imedto.Backend.Contracts.Admin.Auth.Results;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Controllers.Admin;

/// <summary>
/// Testes unitários do endpoint POST /api/admin/auth/change-password.
/// Briefing 2026-06-16_002 — CA1 a CA10.
///
/// Instancia o controller diretamente com InMemory DbContext + mocks,
/// simulando os dois fluxos via ClaimsPrincipal: regular (sem must_reset_password)
/// e força-reset (com must_reset_password = true).
///
/// Nota: lógica de autorização (policy ImedtoAdminChangePassword) e rate limit
/// são validadas pelo Integration Test existente (AdminCookieContextTests) e pelo
/// QA E2E. Aqui testamos apenas as regras de negócio internas do handler.
/// </summary>
[TestFixture]
public class AdminChangePasswordControllerTests
{
    // ── Chave de teste (mesma dos outros testes de admin — nunca vai a prod) ──
    private const string TestPrivateKeyPem = """
        -----BEGIN PRIVATE KEY-----
        MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQg4rVhbCXKM+68CDIH
        LnPByf2Mh9DSwshlql9sTciawGKhRANCAARfibrvziUu4MYmCjD+a8slWe2e6bQO
        NdAXlzYAoIP93/B+BmfjGg0K5FeJd/VdFA8SeYfHlPPa2l4EPtDnvcs4
        -----END PRIVATE KEY-----
        """;

    private const string SenhaAtualHash = "$bcrypt$dummy$hash_atual";
    private const string SenhaNovaHash  = "$bcrypt$dummy$hash_novo";

    private AppDbContext _db;
    private ImedtoAdminRepository _adminRepo;
    private ImedtoAdminRefreshTokenRepository _refreshRepo;
    private ImedtoAdminAuditWriter _audit;
    private Mock<IPasswordHasher> _hasher;
    private ImedtoAdminTokenIssuer _tokenIssuer;
    private Mock<IWebHostEnvironment> _env;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _adminRepo   = new ImedtoAdminRepository(_db);
        _refreshRepo = new ImedtoAdminRefreshTokenRepository(_db);
        _audit       = new ImedtoAdminAuditWriter(_db, new HttpContextAccessor(), NullLogger<ImedtoAdminAuditWriter>.Instance);

        _hasher = new Mock<IPasswordHasher>();

        var jwtOptions = Options.Create(new JwtAuthOptions
        {
            Issuer = "imedto-backend",
            Audience = "imedto-app",
            PrivateKeyPem = TestPrivateKeyPem,
            PublicKeyPem = string.Empty,
            AccessTokenLifetime = TimeSpan.FromMinutes(15),
            RefreshTokenLifetime = TimeSpan.FromDays(30)
        });
        _tokenIssuer = new ImedtoAdminTokenIssuer(
            new Mock<IJwtTokenIssuer>().Object,
            jwtOptions,
            NullLogger<ImedtoAdminTokenIssuer>.Instance);

        _env = new Mock<IWebHostEnvironment>();
        // Modo dev: política de senha = 6 chars. Facilita criar senhas de teste.
        _env.Setup(e => e.EnvironmentName).Returns("Development");
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ImedtoAdmin> CriarAdminNoBanco(string senhaHash = SenhaAtualHash)
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin Teste", senhaHash, forcePasswordReset: false);
        _adminRepo.Adicionar(admin);
        await _db.SaveChangesAsync();
        return admin;
    }

    /// <summary>Cria controller com claims de admin REGULAR (sem must_reset_password).</summary>
    private AdminAuthController CriarControllerRegular(Guid adminId)
    {
        var claims = new List<Claim>
        {
            new("sub", adminId.ToString()),
            new("imedto_admin", "true")
            // SEM must_reset_password
        };
        return CriarControllerComClaims(claims);
    }

    /// <summary>Cria controller com claims de força-reset (must_reset_password = true).</summary>
    private AdminAuthController CriarControllerForceReset(Guid adminId)
    {
        var claims = new List<Claim>
        {
            new("sub", adminId.ToString()),
            new("imedto_admin", "true"),
            new("must_reset_password", "true")
        };
        return CriarControllerComClaims(claims);
    }

    private AdminAuthController CriarControllerComClaims(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpCtx = new DefaultHttpContext();
        httpCtx.User = principal;
        httpCtx.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
        httpCtx.Request.Headers.UserAgent = "teste-unit";

        var controller = new AdminAuthController(
            _tokenIssuer,
            _adminRepo,
            _refreshRepo,
            _audit,
            _hasher.Object,
            _env.Object,
            _db);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpCtx
        };

        return controller;
    }

    // ── CA1 — Caminho feliz: troca voluntária ─────────────────────────────────

    [Test]
    public async Task CA1_TrocaVoluntaria_SenhaCorreta_Retorna200EAtualizaHash()
    {
        var admin = await CriarAdminNoBanco();
        var hashAntes = admin.SenhaHash;

        _hasher.Setup(h => h.Verificar("senhaAtual123", SenhaAtualHash)).Returns(true);
        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        var controller = CriarControllerRegular(admin.Id);
        var result = await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456", "senhaAtual123"),
            CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());

        // Hash deve ter mudado.
        var adminAtualizado = await _db.ImedtoAdmins.FindAsync(admin.Id);
        Assert.That(adminAtualizado!.SenhaHash, Is.EqualTo(SenhaNovaHash));
        Assert.That(adminAtualizado.SenhaHash, Is.Not.EqualTo(hashAntes));

        // ForcePasswordReset deve estar false após a troca.
        Assert.That(adminAtualizado.ForcePasswordReset, Is.False);

        // Audit ALTERAR_SENHA_PROPRIA deve ter sido registrado.
        var audit = _db.ImedtoAdminAuditLogs
            .FirstOrDefault(a => a.Acao == AcoesAuditAdmin.AlterarSenhaPropria);
        Assert.That(audit, Is.Not.Null, "Audit ALTERAR_SENHA_PROPRIA deve ter sido registrado.");
        Assert.That(audit!.AdminId, Is.EqualTo(admin.Id));
        Assert.That(audit.RecursoTipo, Is.EqualTo("admin"));
    }

    // ── CA2 — Senha atual incorreta ───────────────────────────────────────────

    [Test]
    public async Task CA2_TrocaVoluntaria_SenhaAtualIncorreta_LancaBusinessException()
    {
        var admin = await CriarAdminNoBanco();

        _hasher.Setup(h => h.Verificar("senhaErrada", SenhaAtualHash)).Returns(false);

        var controller = CriarControllerRegular(admin.Id);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            controller.ChangePassword(
                new AdminChangePasswordRequest("novaSenha456", "senhaErrada"),
                CancellationToken.None));

        // Mensagem genérica (CA2) — não vaza qual validação falhou.
        Assert.That(ex!.Message, Is.EqualTo("Senha inválida."));

        // Sem alteração de hash.
        var adminNoBanco = await _db.ImedtoAdmins.FindAsync(admin.Id);
        Assert.That(adminNoBanco!.SenhaHash, Is.EqualTo(SenhaAtualHash));
    }

    // ── CA3 — Nova senha fraca ────────────────────────────────────────────────

    [Test]
    public async Task CA3_TrocaVoluntaria_NovaSenhaFraca_LancaBusinessException()
    {
        var admin = await CriarAdminNoBanco();

        _hasher.Setup(h => h.Verificar("senhaAtual123", SenhaAtualHash)).Returns(true);

        var controller = CriarControllerRegular(admin.Id);

        // Em dev: mínimo 6 chars; "12345" tem 5.
        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            controller.ChangePassword(
                new AdminChangePasswordRequest("12345", "senhaAtual123"),
                CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("mínimo"));

        // Hash não alterado.
        var adminNoBanco = await _db.ImedtoAdmins.FindAsync(admin.Id);
        Assert.That(adminNoBanco!.SenhaHash, Is.EqualTo(SenhaAtualHash));
    }

    // ── CA4 — Nova senha == atual ─────────────────────────────────────────────

    [Test]
    public async Task CA4_TrocaVoluntaria_NovaSenhaIgualAtual_LancaBusinessException()
    {
        var admin = await CriarAdminNoBanco();

        _hasher.Setup(h => h.Verificar("mesmasenha", SenhaAtualHash)).Returns(true);

        var controller = CriarControllerRegular(admin.Id);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            controller.ChangePassword(
                new AdminChangePasswordRequest("mesmasenha", "mesmasenha"),
                CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("diferente"));

        var adminNoBanco = await _db.ImedtoAdmins.FindAsync(admin.Id);
        Assert.That(adminNoBanco!.SenhaHash, Is.EqualTo(SenhaAtualHash));
    }

    // ── CA6 — Força-reset NÃO regrediu ───────────────────────────────────────

    [Test]
    public async Task CA6_ForceReset_SemSenhaAtual_Retorna200_SemExigirSenhaAtual()
    {
        // Admin com force-reset — não informar senhaAtual (como faz AdminChangePassword.vue).
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", SenhaAtualHash, forcePasswordReset: true);
        _adminRepo.Adicionar(admin);
        await _db.SaveChangesAsync();

        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        var controller = CriarControllerForceReset(admin.Id);
        var result = await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456", SenhaAtual: null),
            CancellationToken.None);

        // Deve retornar 200 SEM exigir SenhaAtual.
        Assert.That(result, Is.TypeOf<OkObjectResult>());

        var adminAtualizado = await _db.ImedtoAdmins.FindAsync(admin.Id);
        Assert.That(adminAtualizado!.SenhaHash, Is.EqualTo(SenhaNovaHash));

        // ForcePasswordReset zerado após a troca.
        Assert.That(adminAtualizado.ForcePasswordReset, Is.False);

        // Audit deve ser RESET_SENHA_PROPRIA (não ALTERAR_SENHA_PROPRIA).
        var auditReset = _db.ImedtoAdminAuditLogs
            .FirstOrDefault(a => a.Acao == AcoesAuditAdmin.ResetSenhaPropria);
        Assert.That(auditReset, Is.Not.Null, "Audit RESET_SENHA_PROPRIA deve ter sido registrado no força-reset.");

        var auditAlterar = _db.ImedtoAdminAuditLogs
            .FirstOrDefault(a => a.Acao == AcoesAuditAdmin.AlterarSenhaPropria);
        Assert.That(auditAlterar, Is.Null, "Audit ALTERAR_SENHA_PROPRIA NÃO deve aparecer no força-reset.");
    }

    [Test]
    public async Task CA6_ForceReset_SenhaAtualInformadaMasIgnorada_Retorna200()
    {
        // Mesmo se SenhaAtual for enviada no força-reset, deve ser ignorada
        // e a troca deve funcionar independentemente do valor.
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", SenhaAtualHash, forcePasswordReset: true);
        _adminRepo.Adicionar(admin);
        await _db.SaveChangesAsync();

        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);
        // NÃO configura Verificar — não deve ser chamado no fluxo de força-reset.

        var controller = CriarControllerForceReset(admin.Id);
        var result = await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456", "qualquerCoisa"),
            CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());

        // Verificar nunca chamado no fluxo de força-reset.
        _hasher.Verify(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>()), Times.Never,
            "IPasswordHasher.Verificar NÃO deve ser chamado no fluxo de força-reset.");
    }

    // ── CA7 — Admin regular acessa troca voluntária (sem must_reset_password) ──

    [Test]
    public async Task CA7_AdminRegular_SemMustResetPassword_PodeAcessarTrocaVoluntaria()
    {
        var admin = await CriarAdminNoBanco();

        _hasher.Setup(h => h.Verificar("senhaAtual123", SenhaAtualHash)).Returns(true);
        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        // Controller criado SEM a claim must_reset_password → fluxo voluntário.
        var controller = CriarControllerRegular(admin.Id);
        var result = await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456", "senhaAtual123"),
            CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        _hasher.Verify(h => h.Verificar("senhaAtual123", SenhaAtualHash), Times.Once,
            "Senha atual deve ser verificada no fluxo voluntário.");
    }

    // ── CA9 — Audit distinto (voluntária vs força-reset) ─────────────────────

    [Test]
    public async Task CA9_TrocaVoluntaria_AuditComAcaoAlterarSenhaPropria()
    {
        var admin = await CriarAdminNoBanco();

        _hasher.Setup(h => h.Verificar("senhaAtual123", SenhaAtualHash)).Returns(true);
        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        var controller = CriarControllerRegular(admin.Id);
        await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456", "senhaAtual123"),
            CancellationToken.None);

        var log = _db.ImedtoAdminAuditLogs.Single(a => a.Acao == AcoesAuditAdmin.AlterarSenhaPropria);
        Assert.That(log.AdminId, Is.EqualTo(admin.Id));
        Assert.That(log.RecursoTipo, Is.EqualTo("admin"));
        Assert.That(log.RecursoId, Is.EqualTo(admin.Id.ToString()));
        // Garantir sem PII (senha NÃO deve estar em nenhum campo de audit).
        Assert.That(log.Motivo, Is.Null.Or.Empty);
        Assert.That(log.PayloadJson, Is.Null.Or.Empty);
    }

    [Test]
    public async Task CA9_ForceReset_AuditComAcaoResetSenhaPropria()
    {
        var admin = ImedtoAdmin.Criar("admin@imedto.com", "Admin", SenhaAtualHash, forcePasswordReset: true);
        _adminRepo.Adicionar(admin);
        await _db.SaveChangesAsync();

        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        var controller = CriarControllerForceReset(admin.Id);
        await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456"),
            CancellationToken.None);

        var log = _db.ImedtoAdminAuditLogs.Single(a => a.Acao == AcoesAuditAdmin.ResetSenhaPropria);
        Assert.That(log.AdminId, Is.EqualTo(admin.Id));
    }

    // ── CA10 — Multi-tenant não se aplica ────────────────────────────────────

    [Test]
    public async Task CA10_TrocaVoluntaria_SemEstabelecimentoId_SemClaimTenant()
    {
        var admin = await CriarAdminNoBanco();

        _hasher.Setup(h => h.Verificar("senhaAtual123", SenhaAtualHash)).Returns(true);
        _hasher.Setup(h => h.Hash("novaSenha456")).Returns(SenhaNovaHash);

        // Claims SEM estabelecimento_id — admin global.
        var controller = CriarControllerRegular(admin.Id);

        // Verificar por inspeção: as claims do User não contêm estabelecimento_id.
        var temTenantClaim = controller.User.FindFirst("estabelecimento_id");
        Assert.That(temTenantClaim, Is.Null,
            "Admin global NÃO deve ter claim de estabelecimento_id.");

        var result = await controller.ChangePassword(
            new AdminChangePasswordRequest("novaSenha456", "senhaAtual123"),
            CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());

        // Audit log não deve ter TenantAfetadoId.
        var log = _db.ImedtoAdminAuditLogs.Single(a => a.Acao == AcoesAuditAdmin.AlterarSenhaPropria);
        Assert.That(log.TenantAfetadoId, Is.Null,
            "Audit de troca de senha admin não deve referenciar tenant.");
    }

    // ── SenhaAtual ausente no fluxo voluntário ────────────────────────────────

    [Test]
    public async Task TrocaVoluntaria_SemSenhaAtual_LancaBusinessException()
    {
        var admin = await CriarAdminNoBanco();

        var controller = CriarControllerRegular(admin.Id);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            controller.ChangePassword(
                new AdminChangePasswordRequest("novaSenha456", null),
                CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("senha atual"));
    }

    [Test]
    public async Task TrocaVoluntaria_SenhaAtualVazia_LancaBusinessException()
    {
        var admin = await CriarAdminNoBanco();

        var controller = CriarControllerRegular(admin.Id);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            controller.ChangePassword(
                new AdminChangePasswordRequest("novaSenha456", ""),
                CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("senha atual"));
    }
}
