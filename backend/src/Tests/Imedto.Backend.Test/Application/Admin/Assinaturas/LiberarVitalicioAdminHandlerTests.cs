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
public class LiberarVitalicioAdminHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();
    private const long _eid = 10L;

    private AppDbContext _db = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private Mock<IImedtoPlanoRepository> _planoRepoMock = null!;
    private Mock<IAssinaturaService> _assinaturaServiceMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private LiberarVitalicioAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _assinaturaRepoMock = new Mock<IImedtoAssinaturaRepository>();
        _planoRepoMock = new Mock<IImedtoPlanoRepository>();
        _assinaturaServiceMock = new Mock<IAssinaturaService>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new LiberarVitalicioAdminCommandHandler(
            _assinaturaRepoMock.Object,
            _planoRepoMock.Object,
            _audit,
            _assinaturaServiceMock.Object,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_MotivoVazio_LancaBusinessException()
    {
        var cmd = new LiberarVitalicioAdminCommand(_eid, Guid.NewGuid(), "", _adminId);
        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_PlanoNaoEncontrado_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoPlano?)null);

        var cmd = new LiberarVitalicioAdminCommand(_eid, planoId, "motivo", _adminId);
        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_PlanoInativo_LancaBusinessException()
    {
        var plano = ImedtoPlano.Criar("Plano X", null, null, false, "{}", _adminId);
        plano.Inativar();
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var cmd = new LiberarVitalicioAdminCommand(_eid, plano.Id, "motivo", _adminId);
        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_SemVigente_CriaVitalicioEInvalidaCache()
    {
        var plano = ImedtoPlano.Criar("Plano Vitalicio", null, null, false, "{}", _adminId);
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var cmd = new LiberarVitalicioAdminCommand(_eid, plano.Id, "liberando vitalicio", _adminId);
        await _handler.Handle(cmd);

        // Deve adicionar nova assinatura sem expiraEm
        _assinaturaRepoMock.Verify(r => r.Adicionar(It.Is<ImedtoAssinatura>(a =>
            a.EstabelecimentoId == _eid && a.PlanoId == plano.Id && a.ExpiraEm == null)), Times.Once);

        // CA32: cache deve ter sido invalidado
        _assinaturaServiceMock.Verify(s => s.InvalidarCache(_eid), Times.Once);
    }

    [Test]
    public async Task Handle_ComVigente_FechaAnteriorECriaVitalicioEInvalidaCache()
    {
        var planoAntigo = ImedtoPlano.Criar("Plano Trial", null, null, false, "{}", _adminId);
        var planoNovo = ImedtoPlano.Criar("Plano Vitalicio", null, null, false, "{}", _adminId);
        var vigente = ImedtoAssinatura.Criar(_eid, planoAntigo.Id, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(7));

        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoNovo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoNovo);
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoAntigo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoAntigo);
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigente);

        var cmd = new LiberarVitalicioAdminCommand(_eid, planoNovo.Id, "upgrade para vitalicio", _adminId);
        await _handler.Handle(cmd);

        // Vigente anterior deve ter sido fechado
        Assert.That(vigente.EstaVigente(), Is.False);
        _assinaturaRepoMock.Verify(r => r.Atualizar(vigente), Times.Once);

        // Nova vigência criada com expiraEm null (vitalícia)
        _assinaturaRepoMock.Verify(r => r.Adicionar(It.Is<ImedtoAssinatura>(a =>
            a.ExpiraEm == null && a.PlanoId == planoNovo.Id)), Times.Once);

        _assinaturaServiceMock.Verify(s => s.InvalidarCache(_eid), Times.Once);
    }
}
