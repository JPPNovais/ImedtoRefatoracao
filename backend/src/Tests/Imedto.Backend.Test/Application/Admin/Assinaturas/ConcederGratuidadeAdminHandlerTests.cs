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
public class ConcederGratuidadeAdminHandlerTests
{
    private static readonly Guid _idGratuidadeVitalicia = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _adminId = Guid.NewGuid();
    private const long _eid = 2L;

    private AppDbContext _db = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private Mock<IImedtoPlanoRepository> _planoRepoMock = null!;
    private Mock<IAssinaturaService> _assinaturaServiceMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private ConcederGratuidadeAdminCommandHandler _handler = null!;

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

        // Seed do plano Gratuidade Vitalícia no mock.
        var planoGratuidade = ImedtoPlano.Criar("Gratuidade Vitalícia", null, 0, true, "{}", null);
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(_idGratuidadeVitalicia, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoGratuidade);

        _handler = new ConcederGratuidadeAdminCommandHandler(
            _assinaturaRepoMock.Object,
            _planoRepoMock.Object,
            _audit,
            _assinaturaServiceMock.Object,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_MotivoGratuidadeCurto_LancaBusinessException()
    {
        // < 10 chars
        var cmd = new ConcederGratuidadeAdminCommand(_eid, "curto", null, "motivo", _adminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("10 caracteres"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_MotivoVazio_LancaBusinessException()
    {
        var gratuidadeMotivo = new string('a', 20);
        var cmd = new ConcederGratuidadeAdminCommand(_eid, gratuidadeMotivo, null, "", _adminId);

        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_MotivoValido_CriaAssinaturaGratuidadeERegistraAudit()
    {
        var gratuidadeMotivo = "Parceiro estratégico beta tester";
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var cmd = new ConcederGratuidadeAdminCommand(_eid, gratuidadeMotivo, null, "concessão acordada", _adminId);

        await _handler.Handle(cmd);

        _assinaturaRepoMock.Verify(r => r.Adicionar(It.Is<ImedtoAssinatura>(a =>
            a.EstabelecimentoId == _eid &&
            a.PlanoId == _idGratuidadeVitalicia &&
            a.Gratuita == true &&
            a.Motivo == gratuidadeMotivo)), Times.Once);

        var logs = await _db.ImedtoAdminAuditLogs.ToListAsync();
        Assert.That(logs, Has.Count.EqualTo(1));
        Assert.That(logs[0].Acao, Is.EqualTo(AcoesAuditAdmin.ConcederGratuidade));
    }
}
