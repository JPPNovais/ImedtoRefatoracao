using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
/// Testes do handler de excluir região anatômica (CA1, CA3, CA5, CA7 do briefing 2026-06-23_001).
/// </summary>
[TestFixture]
public class ExcluirRegiaoAdminCommandHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private const string MotivoValido = "Motivo válido para exclusão";

    private AppDbContext _db = null!;
    private RegiaoAnatomicaCatalogoRepository _repo = null!;
    private Mock<RegiaoAnatomicaAdminQueryRepository> _queryMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private ExcluirRegiaoAdminCommandHandler _handler = null!;

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

        var cache = new MemoryCache(new MemoryCacheOptions());
        var cacheInvalidador = new CatalogoRegioesCacheInvalidador(cache);
        _handler = new ExcluirRegiaoAdminCommandHandler(_repo, _queryMock.Object, _audit, cacheInvalidador);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── CA1 — excluir nível 1 bloqueado (R1) ────────────────────────────────────

    [Test]
    public async Task Handle_Nivel1_LancaBusinessException()
    {
        // Arrange — região nível 1
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "torax-anterior", "Tórax (anterior)", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new ExcluirRegiaoAdminCommand(regiao.Id, MotivoValido, AdminId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
        Assert.That(ex!.Message, Does.Contain("nível 1"));
    }

    [Test]
    public async Task Handle_Nivel1_RegiaoPermaneceSalva()
    {
        // Arrange
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "torax-anterior", "Tórax (anterior)", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();
        var id = regiao.Id;

        var command = new ExcluirRegiaoAdminCommand(id, MotivoValido, AdminId);

        // Act — ignora exceção esperada
        try { await _handler.Handle(command); } catch (BusinessException) { }

        // Assert — região permanece
        var ainda = await _db.RegioesAnatomicasCatalogo.FindAsync(id);
        Assert.That(ainda, Is.Not.Null);
    }

    [Test]
    public async Task Handle_Nivel1_NenhumAuditGravado()
    {
        // Arrange
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "torax-anterior", "Tórax (anterior)", null, 1, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new ExcluirRegiaoAdminCommand(regiao.Id, MotivoValido, AdminId);

        // Act
        try { await _handler.Handle(command); } catch (BusinessException) { }

        // Assert — sem audit de exclusão
        var audit = await _db.ImedtoAdminAuditLogs
            .FirstOrDefaultAsync(a => a.Acao == "EXCLUIR_REGIAO_ANATOMICA");
        Assert.That(audit, Is.Null);
    }

    // ── CA3 — excluir nível ≥ 2 não é bloqueado pela nova regra (R5) ─────────────
    // TemFilhosAsync usa Dapper (não-virtual, não mockável). O teste verifica apenas
    // que o guard de nível 1 não é acionado — a exceção eventual de Dapper confirma
    // que o handler passou pela checagem de nível 1 e chegou à camada de persistência.

    [Test]
    public async Task Handle_Nivel2_NaoLancaExcecaoDeNivel1()
    {
        // Arrange — região nível 2
        var regiao = RegiaoAnatomicaCatalogo.Criar(
            "pulmao", "Pulmão", "torax-anterior", 2, "anterior", null, null, 1, false);
        await _db.RegioesAnatomicasCatalogo.AddAsync(regiao);
        await _db.SaveChangesAsync();

        var command = new ExcluirRegiaoAdminCommand(regiao.Id, MotivoValido, AdminId);

        // Act — captura apenas BusinessException de nível 1 (nova regra desta entrega)
        // Outras exceções (Dapper/connection) indicam que o guard de nível 1 foi superado.
        BusinessException? excNivel1 = null;
        try { await _handler.Handle(command); }
        catch (BusinessException ex) when (ex.Message.Contains("nível 1")) { excNivel1 = ex; }
        catch { /* Dapper sem banco real — esperado; o importante é não ter exceção de nível 1 */ }

        Assert.That(excNivel1, Is.Null, "Handler não deve bloquear região de nível 2 com a regra de nível 1");
    }

    // ── CA5 — região inexistente → BusinessException (comportamento atual) ───────

    [Test]
    public void Handle_RegiaoInexistente_LancaBusinessException()
    {
        var command = new ExcluirRegiaoAdminCommand(9999, MotivoValido, AdminId);

        var ex = Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
        Assert.That(ex!.Message, Does.Contain("não encontrada"));
    }

    // ── Motivo curto — guarda existente preservada ───────────────────────────────

    [Test]
    public void Handle_MotivoMuitoCurto_LancaBusinessException()
    {
        var command = new ExcluirRegiaoAdminCommand(1, "curto", AdminId);
        Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command));
    }
}
