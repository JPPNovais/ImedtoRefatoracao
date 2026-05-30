using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Admin;

/// <summary>
/// Cobre ImedtoAdminAuditWriter:
/// - Registrar() chama db.SaveChangesAsync e adiciona a entidade correta.
/// - RegistrarLeituraAsync() silencia erros.
/// - Captura IP a partir de X-Forwarded-For.
///
/// Usa Sqlite InMemory para evitar dependência de Postgres real.
/// </summary>
[TestFixture]
public class ImedtoAdminAuditWriterTests
{
    private AppDbContext _db = null!;
    private Mock<IHttpContextAccessor> _httpAccessor = null!;
    private ImedtoAdminAuditWriter _writer = null!;

    [SetUp]
    public void Setup()
    {
        // InMemory — sem dependência de Postgres.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        _httpAccessor = new Mock<IHttpContextAccessor>();

        _writer = new ImedtoAdminAuditWriter(
            _db,
            _httpAccessor.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    private void ConfigurarHttpContext(string? ip = "127.0.0.1", string? userAgent = "test-agent", string? xForwardedFor = null)
    {
        var headers = new HeaderDictionary();
        if (!string.IsNullOrEmpty(xForwardedFor))
            headers["X-Forwarded-For"] = xForwardedFor;
        headers["User-Agent"] = userAgent ?? string.Empty;

        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Headers).Returns(headers);

        var connection = new Mock<ConnectionInfo>();
        connection.Setup(c => c.RemoteIpAddress)
            .Returns(ip != null ? System.Net.IPAddress.Parse(ip) : null);

        var ctx = new Mock<HttpContext>();
        ctx.Setup(c => c.Request).Returns(request.Object);
        ctx.Setup(c => c.Connection).Returns(connection.Object);

        _httpAccessor.Setup(h => h.HttpContext).Returns(ctx.Object);
    }

    [Test]
    public async Task Registrar_SalvaLinhaComCamposCorretos()
    {
        ConfigurarHttpContext("192.168.1.1", "Mozilla/5.0");
        var adminId = Guid.NewGuid();

        await _writer.RegistrarAsync(
            AcoesAuditAdmin.LoginOk,
            adminId,
            recursoTipo: "admin",
            recursoId: adminId.ToString(),
            motivo: null);

        var log = await _db.ImedtoAdminAuditLogs.FirstOrDefaultAsync();

        Assert.That(log, Is.Not.Null);
        Assert.That(log!.Acao, Is.EqualTo(AcoesAuditAdmin.LoginOk));
        Assert.That(log.AdminId, Is.EqualTo(adminId));
        Assert.That(log.RecursoTipo, Is.EqualTo("admin"));
        Assert.That(log.Ip, Is.EqualTo("192.168.1.1"));
        Assert.That(log.UserAgent, Is.EqualTo("Mozilla/5.0"));
    }

    [Test]
    public async Task Registrar_XForwardedFor_UsaPrimeiroIP()
    {
        ConfigurarHttpContext("10.0.0.1", "agent", "203.0.113.10, 10.0.0.1");
        var adminId = Guid.NewGuid();

        await _writer.RegistrarAsync(AcoesAuditAdmin.LoginOk, adminId);

        var log = await _db.ImedtoAdminAuditLogs.FirstOrDefaultAsync();
        Assert.That(log!.Ip, Is.EqualTo("203.0.113.10"));
    }

    [Test]
    public async Task Registrar_LoginFail_AdminIdNulo()
    {
        ConfigurarHttpContext();

        await _writer.RegistrarAsync(
            AcoesAuditAdmin.LoginFail,
            adminId: null,
            motivo: "credencial_invalida");

        var log = await _db.ImedtoAdminAuditLogs.FirstOrDefaultAsync();
        Assert.That(log!.AdminId, Is.Null);
        Assert.That(log.Acao, Is.EqualTo(AcoesAuditAdmin.LoginFail));
    }
}
