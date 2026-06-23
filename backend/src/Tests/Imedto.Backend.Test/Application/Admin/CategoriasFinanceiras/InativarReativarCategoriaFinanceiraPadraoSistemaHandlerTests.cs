using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.CategoriasFinanceiras;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.CategoriasFinanceiras;

/// <summary>
/// Testes de InativarCategoriaFinanceiraPadraoSistemaCommandHandler (R4)
/// e ReativarCategoriaFinanceiraPadraoSistemaCommandHandler (R4.1).
/// CA5 (briefing 2026-06-22_003 M3).
/// </summary>
[TestFixture]
public class InativarReativarCategoriaFinanceiraPadraoSistemaHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<ICategoriaFinanceiraPadraoSistemaRepository> _repoMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private InativarCategoriaFinanceiraPadraoSistemaCommandHandler _inativar = null!;
    private ReativarCategoriaFinanceiraPadraoSistemaCommandHandler _reativar = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _repoMock = new Mock<ICategoriaFinanceiraPadraoSistemaRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _inativar = new InativarCategoriaFinanceiraPadraoSistemaCommandHandler(_repoMock.Object, _audit, _db);
        _reativar = new ReativarCategoriaFinanceiraPadraoSistemaCommandHandler(_repoMock.Object, _audit, _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private CategoriaFinanceiraPadraoSistema GlobalAtivo(string nome = "Marketing")
    {
        var c = CategoriaFinanceiraPadraoSistema.Criar(nome, TipoCategoria.Despesa);
        return c;
    }

    private CategoriaFinanceira CopiaEstab(long estabId, string nome = "Marketing")
    {
        var c = CategoriaFinanceira.CriarPadrao(estabId, nome, TipoCategoria.Despesa);
        _db.CategoriasFinanceiras.Add(c);
        return c;
    }

    // --- R4: inativar global reflete nas cópias ---

    [Test]
    public async Task InativarGlobal_RefleteCopiasPadraoAtivas_R4_CA5()
    {
        // Arrange
        var global = GlobalAtivo();
        _repoMock.Setup(r => r.ObterPorIdOuNulo(1L)).ReturnsAsync(global);

        // Duas cópias ativas em estabelecimentos diferentes
        var copia1 = CopiaEstab(10L);
        var copia2 = CopiaEstab(20L);
        await _db.SaveChangesAsync();

        // Act
        await _inativar.Handle(new InativarCategoriaFinanceiraPadraoSistemaCommand(1L, AdminId));

        // Assert
        Assert.That(global.Ativo, Is.False);
        Assert.That(copia1.Ativo, Is.False);
        Assert.That(copia2.Ativo, Is.False);

        var audit = _db.ImedtoAdminAuditLogs.Local.Single();
        Assert.That(audit.Acao, Is.EqualTo("INATIVAR_CATEGORIA_FINANCEIRA_PADRAO_SISTEMA"));
        Assert.That(audit.PayloadJson, Does.Contain("nInstanciasPropagadas"));
    }

    [Test]
    public void InativarGlobal_NaoEncontrado_LancaBusinessException()
    {
        _repoMock.Setup(r => r.ObterPorIdOuNulo(999L)).ReturnsAsync((CategoriaFinanceiraPadraoSistema?)null);

        Assert.ThrowsAsync<BusinessException>(() =>
            _inativar.Handle(new InativarCategoriaFinanceiraPadraoSistemaCommand(999L, AdminId)));
    }

    // --- R4.1: reativar global reflete nas cópias inativas ---

    [Test]
    public async Task ReativarGlobal_RefleteCopiasInativas_R4_1()
    {
        // Arrange: global inativo
        var global = GlobalAtivo();
        global.Inativar();
        _repoMock.Setup(r => r.ObterPorIdOuNulo(1L)).ReturnsAsync(global);

        // Cópia inativa no estab 10
        var copia = CategoriaFinanceira.CriarPadrao(10L, "Marketing", TipoCategoria.Despesa);
        copia.Inativar(); // M5 permite inativar padrão
        _db.CategoriasFinanceiras.Add(copia);
        await _db.SaveChangesAsync();

        // Act
        await _reativar.Handle(new ReativarCategoriaFinanceiraPadraoSistemaCommand(1L, AdminId));

        // Assert
        Assert.That(global.Ativo, Is.True);
        Assert.That(copia.Ativo, Is.True, "R4.1: reativação global reativa cópias inativadas pelo estabelecimento.");

        var audit = _db.ImedtoAdminAuditLogs.Local.Single();
        Assert.That(audit.Acao, Is.EqualTo("REATIVAR_CATEGORIA_FINANCEIRA_PADRAO_SISTEMA"));
    }

    [Test]
    public void ReativarGlobal_NaoEncontrado_LancaBusinessException()
    {
        _repoMock.Setup(r => r.ObterPorIdOuNulo(999L)).ReturnsAsync((CategoriaFinanceiraPadraoSistema?)null);

        Assert.ThrowsAsync<BusinessException>(() =>
            _reativar.Handle(new ReativarCategoriaFinanceiraPadraoSistemaCommand(999L, AdminId)));
    }

    // --- Propagação não afeta cópias de outros nomes/tipos ---

    [Test]
    public async Task InativarGlobal_NaoAfetaCopiasDiferenteNome_R4()
    {
        // Arrange: global "Marketing/Despesa"
        var global = GlobalAtivo("Marketing");
        _repoMock.Setup(r => r.ObterPorIdOuNulo(1L)).ReturnsAsync(global);

        // Cópia de "Marketing" (deve ser inativada)
        var copiaMarketing = CopiaEstab(10L, "Marketing");
        // Cópia de "Aluguel" (não deve ser tocada)
        var copiaAluguel = CategoriaFinanceira.CriarPadrao(10L, "Aluguel", TipoCategoria.Despesa);
        _db.CategoriasFinanceiras.Add(copiaAluguel);
        await _db.SaveChangesAsync();

        // Act
        await _inativar.Handle(new InativarCategoriaFinanceiraPadraoSistemaCommand(1L, AdminId));

        // Assert
        Assert.That(copiaMarketing.Ativo, Is.False);
        Assert.That(copiaAluguel.Ativo, Is.True, "Cópia de outro nome não deve ser afetada.");
    }
}
