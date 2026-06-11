using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Planos;
using Imedto.Backend.Contracts.Admin.Planos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.Planos;

/// <summary>
/// Testes adicionados na F4 para cobrir FeaturesJson e invalidação de cache em massa (CA28/CA32).
/// </summary>
[TestFixture]
public class AtualizarPlanoAdminHandlerF4Tests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<IImedtoPlanoRepository> _planoRepoMock = null!;
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepoMock = null!;
    private Mock<IAssinaturaService> _assinaturaServiceMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private AtualizarPlanoAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _planoRepoMock = new Mock<IImedtoPlanoRepository>();
        _assinaturaRepoMock = new Mock<IImedtoAssinaturaRepository>();
        _assinaturaServiceMock = new Mock<IAssinaturaService>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new AtualizarPlanoAdminCommandHandler(
            _planoRepoMock.Object,
            _assinaturaRepoMock.Object,
            _assinaturaServiceMock.Object,
            _audit,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_FeaturesJsonAtualizaNoAggregate()
    {
        var plano = ImedtoPlano.Criar("Plano Pro", null, null, false, "{}", _adminId);
        var featuresJson = "{\"receitas\":true,\"ia\":true}";

        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);
        _planoRepoMock.Setup(r => r.ExisteNomeAsync("Plano Pro", plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assinaturaRepoMock.Setup(r => r.ListarEstabelecimentosComPlanoAtivoAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        var cmd = new AtualizarPlanoAdminCommand(plano.Id, "Plano Pro", null, null, false, "{}", featuresJson, "atualização F4", _adminId);
        await _handler.Handle(cmd);

        // FeaturesJson deve ter sido aplicado ao aggregate
        Assert.That(plano.FeaturesJson, Is.EqualTo(featuresJson));
        Assert.That(plano.TemFeature("receitas"), Is.True);
        Assert.That(plano.TemFeature("ia"), Is.True);
        Assert.That(plano.TemFeature("orcamento_completo"), Is.False);
    }

    [Test]
    public async Task Handle_ComEstabelecimentosAtivos_InvalidaCacheParaCadaUm()
    {
        var plano = ImedtoPlano.Criar("Plano Clinica", null, null, false, "{}", _adminId);
        var eids = new List<long> { 1L, 2L, 3L };

        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);
        _planoRepoMock.Setup(r => r.ExisteNomeAsync("Plano Clinica", plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assinaturaRepoMock.Setup(r => r.ListarEstabelecimentosComPlanoAtivoAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eids);

        var cmd = new AtualizarPlanoAdminCommand(plano.Id, "Plano Clinica", null, null, false, "{}", "{\"relatorios_avancados\":true}", "motivo", _adminId);
        await _handler.Handle(cmd);

        // CA32: todos os estabelecimentos devem ter o cache invalidado
        foreach (var eid in eids)
            _assinaturaServiceMock.Verify(s => s.InvalidarCache(eid), Times.Once);

        Assert.That(_assinaturaServiceMock.Invocations.Count(i => i.Method.Name == "InvalidarCache"), Is.EqualTo(3));
    }

    [Test]
    public async Task Handle_SemEstabelecimentosAtivos_NaoInvalidaCache()
    {
        var plano = ImedtoPlano.Criar("Plano Novo", null, null, false, "{}", _adminId);

        _planoRepoMock.Setup(r => r.ObterPorIdAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);
        _planoRepoMock.Setup(r => r.ExisteNomeAsync("Plano Novo", plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _assinaturaRepoMock.Setup(r => r.ListarEstabelecimentosComPlanoAtivoAsync(plano.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        var cmd = new AtualizarPlanoAdminCommand(plano.Id, "Plano Novo", null, null, false, "{}", "{}", "motivo", _adminId);
        await _handler.Handle(cmd);

        _assinaturaServiceMock.Verify(s => s.InvalidarCache(It.IsAny<long>()), Times.Never);
    }
}
