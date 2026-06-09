using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Regioes;
using Imedto.Backend.Domain.Catalogo;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.Regioes;

/// <summary>
/// Testa a trava "filho de nó circunferencial" no CriarRegiaoAdminCommandHandler
/// (CA6b do briefing 2026-06-08_007 — R7: BusinessException 422).
/// RegiaoAnatomicaAdminQueryRepository usa Dapper/Postgres real — substituído por mock.
/// </summary>
[TestFixture]
public class CriarRegiaoAdminCircunferencialGuardTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private RegiaoAnatomicaCatalogoRepository _repo = null!;
    private Mock<RegiaoAnatomicaAdminQueryRepository> _queryMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private CriarRegiaoAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new RegiaoAnatomicaCatalogoRepository(_db);
        _queryMock = new Mock<RegiaoAnatomicaAdminQueryRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new CriarRegiaoAdminCommandHandler(_repo, _queryMock.Object, _audit);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<RegiaoAnatomicaCatalogo> SeedPaiCircunferencial()
    {
        var pai = RegiaoAnatomicaCatalogo.Criar(
            "TORAX-CIRCUNFERENCIAL", "Tórax circunferencial", null, 1,
            "circunferencial", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(pai);
        await _db.SaveChangesAsync();
        return pai;
    }

    private async Task<RegiaoAnatomicaCatalogo> SeedPaiAnterior()
    {
        var pai = RegiaoAnatomicaCatalogo.Criar(
            "TORAX-ANTERIOR", "Tórax anterior", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(pai);
        await _db.SaveChangesAsync();
        return pai;
    }

    [Test]
    public async Task Handle_PaiCircunferencial_Lanca422ComMensagemCorreta()
    {
        // Arrange
        await SeedPaiCircunferencial();
        _queryMock.Setup(q => q.ExisteCodigoAsync("TORAX-CIRC-FILHO", 0, default)).ReturnsAsync(false);

        var command = new CriarRegiaoAdminCommand(
            Codigo: "TORAX-CIRC-FILHO",
            Nome: "Filho inválido",
            PaiCodigo: "TORAX-CIRCUNFERENCIAL",
            Nivel: 2,
            Vista: "circunferencial",
            TemplateTexto: null,
            Ordem: 1,
            Lateralidade: false,
            Motivo: "Tentativa de criar filho de nó circunferencial",
            AdminId: AdminId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
        Assert.That(ex!.Message, Is.EqualTo("Nós circunferenciais são agregadores e não aceitam sub-regiões."));
    }

    [Test]
    public async Task Handle_PaiCircunferencial_NaoPersisteFilho()
    {
        // Arrange
        await SeedPaiCircunferencial();
        var countAntes = await _db.RegioesAnatomicasCatalogo.CountAsync();
        _queryMock.Setup(q => q.ExisteCodigoAsync("FILHO-CIRC", 0, default)).ReturnsAsync(false);

        var command = new CriarRegiaoAdminCommand(
            Codigo: "FILHO-CIRC",
            Nome: "Filho inválido",
            PaiCodigo: "TORAX-CIRCUNFERENCIAL",
            Nivel: 2,
            Vista: "circunferencial",
            TemplateTexto: null,
            Ordem: 1,
            Lateralidade: false,
            Motivo: "Teste de trava de circunferencial",
            AdminId: AdminId);

        // Act — ignora a exceção propositalmente
        try { await _handler.Handle(command); } catch (BusinessException) { }

        // Assert — nenhum novo registro criado além do pai que já estava
        var countDepois = await _db.RegioesAnatomicasCatalogo.CountAsync();
        Assert.That(countDepois, Is.EqualTo(countAntes));
    }

    [Test]
    public async Task Handle_PaiNaoCircunferencial_CriaNormalmente()
    {
        // Arrange — pai com vista "anterior"
        await SeedPaiAnterior();
        _queryMock.Setup(q => q.ExisteCodigoAsync("TORAX-ANT-FILHO", 0, default)).ReturnsAsync(false);

        var command = new CriarRegiaoAdminCommand(
            Codigo: "TORAX-ANT-FILHO",
            Nome: "Filho válido",
            PaiCodigo: "TORAX-ANTERIOR",
            Nivel: 2,
            Vista: "anterior",
            TemplateTexto: null,
            Ordem: 1,
            Lateralidade: false,
            Motivo: "Teste de criação com pai não circunferencial",
            AdminId: AdminId);

        // Act — deve funcionar sem exceção
        var id = await _handler.Handle(command);

        // Assert
        Assert.That(id, Is.GreaterThan(0));
        var criado = await _db.RegioesAnatomicasCatalogo.FindAsync(id);
        Assert.That(criado, Is.Not.Null);
        Assert.That(criado!.PaiCodigo, Is.EqualTo("TORAX-ANTERIOR"));
    }
}
