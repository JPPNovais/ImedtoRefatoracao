using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Planos;
using Imedto.Backend.Contracts.Admin.Planos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.Planos;

[TestFixture]
public class DesativarPlanoAdminHandlerTests
{
    private static readonly Guid _idGratuidadeVitalicia = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<IImedtoPlanoRepository> _planoRepoMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private DesativarPlanoAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _planoRepoMock = new Mock<IImedtoPlanoRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new DesativarPlanoAdminCommandHandler(_planoRepoMock.Object, _audit, _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_PlanoGratuidadeVitalicia_LancaBusinessException()
    {
        var cmd = new DesativarPlanoAdminCommand(_idGratuidadeVitalicia, "motivo", _adminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("Gratuidade Vitalícia"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_MotivoVazio_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        var cmd = new DesativarPlanoAdminCommand(planoId, "", _adminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("Motivo"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_PlanoNaoEncontrado_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoPlano?)null);

        var cmd = new DesativarPlanoAdminCommand(planoId, "motivo valido", _adminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("não encontrado"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_PlanoValido_InativaERegistraAudit()
    {
        var plano = ImedtoPlano.Criar("Plano Teste", null, null, false, "{}", _adminId);
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var cmd = new DesativarPlanoAdminCommand(plano.Id, "motivo de desativação", _adminId);

        await _handler.Handle(cmd);

        Assert.That(plano.Ativo, Is.False);
        _planoRepoMock.Verify(r => r.Atualizar(plano), Times.Once);

        var logs = await _db.ImedtoAdminAuditLogs.ToListAsync();
        Assert.That(logs, Has.Count.EqualTo(1));
        Assert.That(logs[0].Acao, Is.EqualTo(AcoesAuditAdmin.DesativarPlano));
    }
}
