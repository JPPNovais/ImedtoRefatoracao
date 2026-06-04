using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Testes do handler de excluir modelo de permissão padrão do sistema.
/// CA4, CA5 do briefing 2026-06-04_001.
/// </summary>
[TestFixture]
public class ExcluirModeloPermissaoPadraoSistemaHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private ModeloPermissaoRepository _repo = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private ExcluirModeloPermissaoPadraoSistemaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new ModeloPermissaoRepository(_db);

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new ExcluirModeloPermissaoPadraoSistemaCommandHandler(
            _repo, _audit, _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<ModeloPermissaoEstabelecimento> SeedGlobal()
    {
        var global = ModeloPermissaoEstabelecimento.CriarGlobal(
            "Financeiro", TipoAcessoModelo.Recepcionista,
            new[] { "financeiro.ver" }, null, null, null, null);
        _db.ModelosPermissao.Add(global);
        await _db.SaveChangesAsync();
        return global;
    }

    [Test]
    public async Task Handle_SemVinculoAtivo_ExcluiGlobalEGravaAudit()
    {
        // Arrange — CA5: sem vínculos
        var global = await SeedGlobal();
        var copia = ModeloPermissaoEstabelecimento.CriarCopiaDeGlobal(global, 5L);
        _db.ModelosPermissao.Add(copia);
        await _db.SaveChangesAsync();

        // Act
        await _handler.Handle(new ExcluirModeloPermissaoPadraoSistemaCommand(global.Id, _adminId));

        // Assert — registro global e cópia excluídos
        Assert.That(await _db.ModelosPermissao.CountAsync(), Is.EqualTo(0));

        // Audit gravado
        var audit = _db.ImedtoAdminAuditLogs.Single();
        Assert.That(audit.Acao, Is.EqualTo("EXCLUIR_MODELO_PERMISSAO_PADRAO_SISTEMA"));
        Assert.That(audit.PayloadJson, Does.Contain("Financeiro"));
        Assert.That(audit.PayloadJson, Does.Contain("nInstanciasExcluidas"));
    }

    [Test]
    public void Handle_NaoEncontrado_Lanca422()
    {
        Assert.ThrowsAsync<BusinessException>(async () =>
            await _handler.Handle(new ExcluirModeloPermissaoPadraoSistemaCommand(999L, _adminId)));
    }

    /// <summary>CA4: bloqueio quando há vínculo ativo — usa mock para simular vínculo ativo.</summary>
    [Test]
    public async Task Handle_ComVinculoAtivo_Lanca422SemExcluirNada()
    {
        // Arrange — seedar global, mas usar mock para simular vínculo ativo
        var global = await SeedGlobal();

        // Mock do repositório que reporta vínculo ativo
        var repoMock = new Mock<IModeloPermissaoRepository>();
        repoMock.Setup(r => r.ObterGlobalPorIdOuNulo(global.Id)).ReturnsAsync(global);
        repoMock.Setup(r => r.CopiaEstaEmUsoEmQualquerEstabelecimento("Financeiro", default)).ReturnsAsync(true);

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        var audit = new ImedtoAdminAuditWriter(_db, httpMock.Object, NullLogger<ImedtoAdminAuditWriter>.Instance);

        var handler = new ExcluirModeloPermissaoPadraoSistemaCommandHandler(repoMock.Object, audit, _db);

        // Act + Assert
        var ex = Assert.ThrowsAsync<BusinessException>(async () =>
            await handler.Handle(new ExcluirModeloPermissaoPadraoSistemaCommand(global.Id, _adminId)));
        Assert.That(ex!.Message, Does.Contain("vinculados").IgnoreCase);

        // Nenhuma exclusão ocorreu
        Assert.That(await _db.ModelosPermissao.CountAsync(), Is.EqualTo(1));
    }
}
