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

/// <summary>
/// Testes adicionados na F4 para cobrir FeaturesJson no handler de CRIAR plano (CA28 — regressão).
/// O handler omitia featuresJson na chamada ImedtoPlano.Criar() → gravava "{}" silenciosamente.
/// </summary>
[TestFixture]
public class CriarPlanoAdminHandlerF4Tests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<IImedtoPlanoRepository> _planoRepoMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private CriarPlanoAdminCommandHandler _handler = null!;

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

        _handler = new CriarPlanoAdminCommandHandler(
            _planoRepoMock.Object,
            _audit,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_FeaturesJsonRepassadoAoAggregate()
    {
        var featuresJson = "{\"receitas\":true,\"ia\":true}";
        ImedtoPlano? planoSalvo = null;

        _planoRepoMock.Setup(r => r.ExisteNomeAsync("Plano Pro", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _planoRepoMock.Setup(r => r.Adicionar(It.IsAny<ImedtoPlano>()))
            .Callback<ImedtoPlano>(p => planoSalvo = p);

        var cmd = new CriarPlanoAdminCommand(
            "Plano Pro",
            null,
            null,
            false,
            "{}",
            featuresJson,
            "criação F4",
            _adminId);

        await _handler.Handle(cmd);

        Assert.That(planoSalvo, Is.Not.Null, "Plano deveria ter sido adicionado ao repositório.");
        Assert.That(planoSalvo!.FeaturesJson, Is.EqualTo(featuresJson));
        Assert.That(planoSalvo.TemFeature("receitas"), Is.True);
        Assert.That(planoSalvo.TemFeature("ia"), Is.True);
        Assert.That(planoSalvo.TemFeature("orcamento_completo"), Is.False);
    }

    [Test]
    public async Task Handle_FeaturesJsonVazio_GravaChavesFalse()
    {
        ImedtoPlano? planoSalvo = null;

        _planoRepoMock.Setup(r => r.ExisteNomeAsync("Plano Básico", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _planoRepoMock.Setup(r => r.Adicionar(It.IsAny<ImedtoPlano>()))
            .Callback<ImedtoPlano>(p => planoSalvo = p);

        var cmd = new CriarPlanoAdminCommand(
            "Plano Básico",
            null,
            null,
            true,
            "{}",
            "{}",
            "criação básica",
            _adminId);

        await _handler.Handle(cmd);

        Assert.That(planoSalvo, Is.Not.Null);
        Assert.That(planoSalvo!.TemFeature("receitas"), Is.False);
        Assert.That(planoSalvo.TemFeature("ia"), Is.False);
    }
}
