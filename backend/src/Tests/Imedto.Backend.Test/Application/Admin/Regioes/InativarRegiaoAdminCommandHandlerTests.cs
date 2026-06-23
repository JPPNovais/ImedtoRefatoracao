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
/// Testes do handler de inativar região anatômica (CA2, CA4, CA5 do briefing 2026-06-23_001).
/// </summary>
[TestFixture]
public class InativarRegiaoAdminCommandHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private const string MotivoValido = "Motivo válido para inativação";

    private AppDbContext _db = null!;
    private RegiaoAnatomicaCatalogoRepository _repo = null!;
    private Mock<RegiaoAnatomicaAdminQueryRepository> _queryMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private InativarRegiaoAdminCommandHandler _handler = null!;

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

        _handler = new InativarRegiaoAdminCommandHandler(_repo, _queryMock.Object, _audit);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── CA2 — inativar nível 1 bloqueado (R2) ───────────────────────────────────

    [Test]
    public async Task Handle_Nivel1_LancaBusinessException()
    {
        // Arrange — região nível 1 ativa
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "msd-anterior", "Membro superior direito (anterior)", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new InativarRegiaoAdminCommand(regiao.Id, MotivoValido, AdminId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
        Assert.That(ex!.Message, Does.Contain("nível 1"));
    }

    [Test]
    public async Task Handle_Nivel1_RegiaoPermaneceAtiva()
    {
        // Arrange
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "msd-anterior", "Membro superior direito (anterior)", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();
        var id = regiao.Id;

        var command = new InativarRegiaoAdminCommand(id, MotivoValido, AdminId);

        // Act
        try { await _handler.Handle(command); } catch (BusinessException) { }

        // Assert — permanece ativo
        var recarregada = await _db.RegioesAnatomicasCatalogo.FindAsync(id);
        Assert.That(recarregada!.Ativo, Is.True);
    }

    [Test]
    public async Task Handle_Nivel1_NenhumAuditGravado()
    {
        // Arrange
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "msd-anterior", "Membro superior direito (anterior)", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new InativarRegiaoAdminCommand(regiao.Id, MotivoValido, AdminId);

        // Act
        try { await _handler.Handle(command); } catch (BusinessException) { }

        // Assert — sem audit de inativação
        var audit = await _db.ImedtoAdminAuditLogs
            .FirstOrDefaultAsync(a => a.Acao == "INATIVAR_REGIAO_ANATOMICA");
        Assert.That(audit, Is.Null);
    }

    // ── CA4 — inativar nível ≥ 2 segue funcionando ──────────────────────────────

    [Test]
    public async Task Handle_Nivel2_InativaEAudit()
    {
        // Arrange — região nível 2
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "pulmao", "Pulmão", "torax-anterior", 2, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new InativarRegiaoAdminCommand(regiao.Id, MotivoValido, AdminId);

        // Act
        await _handler.Handle(command);

        // Assert — inativo
        var recarregada = await _db.RegioesAnatomicasCatalogo.FindAsync(regiao.Id);
        Assert.That(recarregada!.Ativo, Is.False);

        // Assert — audit gravado
        var audit = await _db.ImedtoAdminAuditLogs
            .FirstOrDefaultAsync(a => a.Acao == "INATIVAR_REGIAO_ANATOMICA");
        Assert.That(audit, Is.Not.Null);
    }

    // ── CA5 — região inexistente → BusinessException (comportamento atual) ───────

    [Test]
    public void Handle_RegiaoInexistente_LancaBusinessException()
    {
        var command = new InativarRegiaoAdminCommand(9999, MotivoValido, AdminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
        Assert.That(ex!.Message, Does.Contain("não encontrada"));
    }

    // ── Motivo curto — guarda existente preservada ───────────────────────────────

    [Test]
    public void Handle_MotivoMuitoCurto_LancaBusinessException()
    {
        var command = new InativarRegiaoAdminCommand(1, "curto", AdminId);
        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
    }
}
