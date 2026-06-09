using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.Regioes;
using Imedto.Backend.Domain.Catalogo;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.Regioes;

/// <summary>
/// Testes do handler de reativar região anatômica (CA9, CA9b do briefing 2026-06-08_007).
/// </summary>
[TestFixture]
public class ReativarRegiaoAdminCommandHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private RegiaoAnatomicaCatalogoRepository _repo = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private ReativarRegiaoAdminCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new RegiaoAnatomicaCatalogoRepository(_db);

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new ReativarRegiaoAdminCommandHandler(_repo, _audit);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<RegiaoAnatomicaCatalogo> SeedRegiaoInativa()
    {
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "TORAX-ANTERIOR", "Tórax anterior", null, 1, "anterior", null, null, 1, false);
        regiao.Inativar();
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();
        return regiao;
    }

    [Test]
    public async Task Handle_CaminhoFeliz_ReativaESalva()
    {
        // Arrange
        var regiao = await SeedRegiaoInativa();
        var command = new ReativarRegiaoAdminCommand(regiao.Id, "Motivo válido para reativação", AdminId);

        // Act
        await _handler.Handle(command);

        // Assert
        var recarregada = await _db.RegioesAnatomicasCatalogo.FindAsync(regiao.Id);
        Assert.That(recarregada!.Ativo, Is.True);
    }

    [Test]
    public async Task Handle_CaminhoFeliz_GravarAuditLog()
    {
        // Arrange
        var regiao = await SeedRegiaoInativa();
        var command = new ReativarRegiaoAdminCommand(regiao.Id, "Motivo válido para reativação", AdminId);

        // Act
        await _handler.Handle(command);

        // Assert — audit gravado
        var audit = await _db.ImedtoAdminAuditLogs
            .FirstOrDefaultAsync(a => a.RecursoId == regiao.Id.ToString() && a.Acao == "REATIVAR_REGIAO_ANATOMICA");
        Assert.That(audit, Is.Not.Null);
        Assert.That(audit!.AdminId, Is.EqualTo(AdminId));
        Assert.That(audit.RecursoTipo, Is.EqualTo("regiao_anatomica"));
        Assert.That(audit.Motivo, Is.EqualTo("Motivo válido para reativação"));
    }

    [Test]
    public void Handle_MotivoVazio_LancaBusinessException()
    {
        var command = new ReativarRegiaoAdminCommand(999, "curto", AdminId);

        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
    }

    [Test]
    public async Task Handle_RegiaoNaoExiste_LancaBusinessException()
    {
        // Arrange — não seed nada, id inexistente
        var command = new ReativarRegiaoAdminCommand(9999, "Motivo válido para reativação", AdminId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
        Assert.That(ex!.Message, Does.Contain("não encontrada"));
    }

    [Test]
    public async Task Handle_RegiaoJaAtiva_LancaBusinessException()
    {
        // Arrange — região ativa (sem inativar)
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "REGIAO-ATIVA", "Região ativa", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new ReativarRegiaoAdminCommand(regiao.Id, "Motivo válido para reativação", AdminId);

        // Act & Assert
        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
    }
}
