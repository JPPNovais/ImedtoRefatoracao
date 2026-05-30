using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Assinaturas;
using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.Assinaturas;

[TestFixture]
public class EncerrarAssinaturaAdminHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private EncerrarAssinaturaAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _assinaturaRepoMock = new Mock<IImedtoAssinaturaRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new EncerrarAssinaturaAdminCommandHandler(
            _assinaturaRepoMock.Object,
            _audit,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_AssinaturaNaoEncontrada_LancaBusinessException()
    {
        var id = Guid.NewGuid();
        _assinaturaRepoMock.Setup(r => r.ObterPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var cmd = new EncerrarAssinaturaAdminCommand(id, "motivo", _adminId);

        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_AssinaturaJaEncerrada_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        var assinatura = ImedtoAssinatura.Criar(1, planoId, false, null, _adminId);
        assinatura.FecharVigencia(); // já encerrada

        _assinaturaRepoMock.Setup(r => r.ObterPorIdAsync(assinatura.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var cmd = new EncerrarAssinaturaAdminCommand(assinatura.Id, "motivo", _adminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("já foi encerrada"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_AssinaturaVigente_EncerrraERegistraAudit()
    {
        var planoId = Guid.NewGuid();
        var assinatura = ImedtoAssinatura.Criar(5, planoId, false, null, _adminId);

        _assinaturaRepoMock.Setup(r => r.ObterPorIdAsync(assinatura.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var cmd = new EncerrarAssinaturaAdminCommand(assinatura.Id, "cliente cancelou", _adminId);

        await _handler.Handle(cmd);

        Assert.That(assinatura.EstaVigente(), Is.False);
        _assinaturaRepoMock.Verify(r => r.Atualizar(assinatura), Times.Once);

        var logs = await _db.ImedtoAdminAuditLogs.ToListAsync();
        Assert.That(logs, Has.Count.EqualTo(1));
        Assert.That(logs[0].Acao, Is.EqualTo(AcoesAuditAdmin.EncerrarAssinatura));
    }
}
