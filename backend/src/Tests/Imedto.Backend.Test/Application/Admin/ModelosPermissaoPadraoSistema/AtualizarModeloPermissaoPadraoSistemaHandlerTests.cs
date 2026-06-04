using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Testes do handler de atualizar modelo de permissão padrão do sistema.
/// CA2, CA18 do briefing 2026-06-04_001.
/// </summary>
[TestFixture]
public class AtualizarModeloPermissaoPadraoSistemaHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private ModeloPermissaoRepository _repo = null!;
    private Mock<ModeloPermissaoPadraoSistemaQueryRepository> _queryMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private AtualizarModeloPermissaoPadraoSistemaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new ModeloPermissaoRepository(_db);
        _queryMock = new Mock<ModeloPermissaoPadraoSistemaQueryRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new AtualizarModeloPermissaoPadraoSistemaCommandHandler(
            _repo, _queryMock.Object, _audit, _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<(ModeloPermissaoEstabelecimento Global, ModeloPermissaoEstabelecimento Copia1, ModeloPermissaoEstabelecimento Copia2)> SeedMedico()
    {
        var global = ModeloPermissaoEstabelecimento.CriarGlobal(
            "Médico", TipoAcessoModelo.Profissional,
            new[] { "agenda.ver", "prontuario.ver" },
            new[] { PermissoesExtras.AssistenteClinicoIa },
            "fa-user-doctor", "hsl(254 56% 38%)", "Médico padrão");

        _db.ModelosPermissao.Add(global);
        await _db.SaveChangesAsync();

        var copia1 = ModeloPermissaoEstabelecimento.CriarCopiaDeGlobal(global, 10L);
        var copia2 = ModeloPermissaoEstabelecimento.CriarCopiaDeGlobal(global, 20L);
        _db.ModelosPermissao.AddRange(copia1, copia2);
        await _db.SaveChangesAsync();

        return (global, copia1, copia2);
    }

    [Test]
    public async Task Handle_CaminhoFeliz_PropagaNovaPermissaoParaCopiasEGravaAudit()
    {
        // Arrange
        var (global, copia1, copia2) = await SeedMedico();

        var command = new AtualizarModeloPermissaoPadraoSistemaCommand(
            global.Id,
            "Médico",
            TipoAcessoModelo.Profissional,
            new[] { "agenda.ver", "prontuario.ver", "relatorios.exportar" },
            "fa-user-doctor", "hsl(254 56% 38%)", "Médico padrão",
            _adminId);

        // Act
        await _handler.Handle(command);

        // Assert — CA2: nova permissão propagada para as cópias
        await _db.Entry(copia1).ReloadAsync();
        await _db.Entry(copia2).ReloadAsync();

        Assert.That(copia1.TemAcao("relatorios", "exportar"), Is.True);
        Assert.That(copia2.TemAcao("relatorios", "exportar"), Is.True);

        // CA18: permissões extras preservadas
        Assert.That(copia1.TemPermissaoExtra(PermissoesExtras.AssistenteClinicoIa), Is.True);

        // Audit gravado
        var audit = _db.ImedtoAdminAuditLogs.Single();
        Assert.That(audit.Acao, Is.EqualTo("ATUALIZAR_MODELO_PERMISSAO_PADRAO_SISTEMA"));
        Assert.That(audit.PayloadJson, Does.Contain("nInstanciasPropagadas"));
    }

    [Test]
    public void Handle_NomeDuplicadoGlobal_Lanca422()
    {
        // Arrange — sem seed no DB, usando repo real que retorna false/true
        var repoMock = new Mock<IModeloPermissaoRepository>();
        var fakeGlobal = ModeloPermissaoEstabelecimento.CriarGlobal("Médico", TipoAcessoModelo.Profissional);

        repoMock.Setup(r => r.ObterGlobalPorIdOuNulo(fakeGlobal.Id)).ReturnsAsync(fakeGlobal);
        repoMock.Setup(r => r.ExisteGlobalComNome("Admin", fakeGlobal.Id, default)).ReturnsAsync(true);

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        var audit = new ImedtoAdminAuditWriter(_db, httpMock.Object, NullLogger<ImedtoAdminAuditWriter>.Instance);

        var handler = new AtualizarModeloPermissaoPadraoSistemaCommandHandler(
            repoMock.Object, _queryMock.Object, audit, _db);

        var command = new AtualizarModeloPermissaoPadraoSistemaCommand(
            fakeGlobal.Id, "Admin", TipoAcessoModelo.Profissional, null, null, null, null, _adminId);

        Assert.ThrowsAsync<BusinessException>(async () => await handler.Handle(command));
    }

    [Test]
    public async Task Handle_GlobalNaoEncontrado_Lanca422()
    {
        // Act + Assert (DB vazio)
        var command = new AtualizarModeloPermissaoPadraoSistemaCommand(
            999L, "X", TipoAcessoModelo.Profissional, null, null, null, null, _adminId);

        Assert.ThrowsAsync<BusinessException>(async () => await _handler.Handle(command));
    }
}
