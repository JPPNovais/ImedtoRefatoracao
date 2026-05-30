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
public class TrocarPlanoAdminHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();
    private const long _eid = 1L;

    private AppDbContext _db = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private Mock<IImedtoPlanoRepository> _planoRepoMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private TrocarPlanoAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _assinaturaRepoMock = new Mock<IImedtoAssinaturaRepository>();
        _planoRepoMock = new Mock<IImedtoPlanoRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new TrocarPlanoAdminCommandHandler(
            _assinaturaRepoMock.Object,
            _planoRepoMock.Object,
            _audit,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_MotivoVazio_LancaBusinessException()
    {
        var cmd = new TrocarPlanoAdminCommand(_eid, Guid.NewGuid(), DateTimeOffset.UtcNow, null, "", _adminId);

        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_PlanoNaoEncontrado_LancaBusinessException()
    {
        var planoId = Guid.NewGuid();
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoPlano?)null);

        var cmd = new TrocarPlanoAdminCommand(_eid, planoId, DateTimeOffset.UtcNow, null, "motivo", _adminId);

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

        var cmd = new TrocarPlanoAdminCommand(_eid, plano.Id, DateTimeOffset.UtcNow, null, "motivo", _adminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(cmd));
        Assert.That(ex!.Message, Does.Contain("inativo"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task Handle_SemAssinaturaVigente_CriaNovaERegistraAudit()
    {
        // Sem vigente: nenhum FecharVigencia necessário.
        var plano = ImedtoPlano.Criar("Plano Pro", null, null, false, "{}", _adminId);
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var cmd = new TrocarPlanoAdminCommand(_eid, plano.Id, DateTimeOffset.UtcNow, null, "troca inicial", _adminId);

        await _handler.Handle(cmd);

        // Verifica que adicionou nova assinatura e registrou audit.
        _assinaturaRepoMock.Verify(r => r.Adicionar(It.Is<ImedtoAssinatura>(a =>
            a.EstabelecimentoId == _eid && a.PlanoId == plano.Id && a.Gratuita == false)), Times.Once);

        var logs = await _db.ImedtoAdminAuditLogs.ToListAsync();
        Assert.That(logs, Has.Count.EqualTo(1));
        Assert.That(logs[0].Acao, Is.EqualTo(AcoesAuditAdmin.TrocarPlano));
    }

    [Test]
    public async Task Handle_ComAssinaturaVigente_FechaAnteriorECriaNovaEmTransacao()
    {
        var planoAntigo = ImedtoPlano.Criar("Plano Básico", null, null, false, "{}", _adminId);
        var planoNovo = ImedtoPlano.Criar("Plano Pro", null, null, false, "{}", _adminId);

        var vigente = ImedtoAssinatura.Criar(_eid, planoAntigo.Id, false, null, _adminId);

        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoNovo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoNovo);
        _planoRepoMock.Setup(r => r.ObterPorIdAsync(planoAntigo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoAntigo);
        _assinaturaRepoMock.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_eid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigente);

        var cmd = new TrocarPlanoAdminCommand(_eid, planoNovo.Id, DateTimeOffset.UtcNow, null, "upgrade", _adminId);

        await _handler.Handle(cmd);

        // Assinatura anterior deve ter sido fechada.
        Assert.That(vigente.EstaVigente(), Is.False);
        _assinaturaRepoMock.Verify(r => r.Atualizar(vigente), Times.Once);
        _assinaturaRepoMock.Verify(r => r.Adicionar(It.IsAny<ImedtoAssinatura>()), Times.Once);
    }
}
