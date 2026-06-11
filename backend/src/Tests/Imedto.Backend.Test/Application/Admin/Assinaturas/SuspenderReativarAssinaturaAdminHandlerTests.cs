using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Assinaturas;
using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.Assinaturas;

[TestFixture]
public class SuspenderReativarAssinaturaAdminHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();
    private const long _eid = 20L;

    private AppDbContext _db = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private Mock<IAssinaturaService> _assinaturaServiceMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private SuspenderAssinaturaAdminCommandHandler _suspenderHandler = null!;
    private ReativarAssinaturaAdminCommandHandler _reativarHandler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _assinaturaRepoMock = new Mock<IImedtoAssinaturaRepository>();
        _assinaturaServiceMock = new Mock<IAssinaturaService>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        // Ordem dos ctors: (assinaturaRepo, audit, assinaturaService, db)
        _suspenderHandler = new SuspenderAssinaturaAdminCommandHandler(
            _assinaturaRepoMock.Object,
            _audit,
            _assinaturaServiceMock.Object,
            _db);

        _reativarHandler = new ReativarAssinaturaAdminCommandHandler(
            _assinaturaRepoMock.Object,
            _audit,
            _assinaturaServiceMock.Object,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Suspender_SemVigente_LancaBusinessException()
    {
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var cmd = new SuspenderAssinaturaAdminCommand(_eid, "motivo", _adminId);
        Assert.ThrowsAsync<BusinessException>(() => _suspenderHandler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Suspender_ComVigente_PreencheSuspensaEmEInvalidaCache()
    {
        var planoId = Guid.NewGuid();
        var vigente = ImedtoAssinatura.Criar(_eid, planoId, false, null, _adminId);
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigente);

        var cmd = new SuspenderAssinaturaAdminCommand(_eid, "inadimplência", _adminId);
        await _suspenderHandler.Handle(cmd);

        // SuspensaEm deve estar preenchido; não abre nova linha
        Assert.That(vigente.SuspensaEm, Is.Not.Null);
        _assinaturaRepoMock.Verify(r => r.Atualizar(vigente), Times.Once);
        _assinaturaRepoMock.Verify(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()), Times.Never);

        // CA32
        _assinaturaServiceMock.Verify(s => s.InvalidarCache(_eid), Times.Once);
    }

    [Test]
    public async Task Suspender_JaSuspenso_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        var vigente = ImedtoAssinatura.Criar(_eid, planoId, false, null, _adminId);
        vigente.Suspender(); // já suspenso

        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigente);

        var cmd = new SuspenderAssinaturaAdminCommand(_eid, "motivo", _adminId);
        Assert.ThrowsAsync<BusinessException>(() => _suspenderHandler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Reativar_ComVigenteSuspenso_LimpaSuspensaEmEInvalidaCache()
    {
        var planoId = Guid.NewGuid();
        var vigente = ImedtoAssinatura.Criar(_eid, planoId, false, null, _adminId);
        vigente.Suspender();

        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigente);

        var cmd = new ReativarAssinaturaAdminCommand(_eid, "problema resolvido", _adminId);
        await _reativarHandler.Handle(cmd);

        Assert.That(vigente.SuspensaEm, Is.Null);
        _assinaturaRepoMock.Verify(r => r.Atualizar(vigente), Times.Once);
        _assinaturaRepoMock.Verify(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()), Times.Never);

        // CA32
        _assinaturaServiceMock.Verify(s => s.InvalidarCache(_eid), Times.Once);
    }

    [Test]
    public async Task Reativar_NaoSuspenso_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        var vigente = ImedtoAssinatura.Criar(_eid, planoId, false, null, _adminId);
        // não suspenso

        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigente);

        var cmd = new ReativarAssinaturaAdminCommand(_eid, "motivo", _adminId);
        Assert.ThrowsAsync<BusinessException>(() => _reativarHandler.Handle(cmd));
        await Task.CompletedTask;
    }
}
