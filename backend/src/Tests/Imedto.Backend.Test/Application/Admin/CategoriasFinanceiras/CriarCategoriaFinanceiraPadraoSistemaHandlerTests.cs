using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Imedto.Backend.Application.Admin.CategoriasFinanceiras;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.CategoriasFinanceiras;

/// <summary>
/// Testes do CriarCategoriaFinanceiraPadraoSistemaCommandHandler.
/// CA4, CA7, CA8 do briefing 2026-06-22_003 M3.
/// </summary>
[TestFixture]
public class CriarCategoriaFinanceiraPadraoSistemaHandlerTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<ICategoriaFinanceiraPadraoSistemaRepository> _repoMock = null!;
    private Mock<ICategoriaFinanceiraRepository> _categoriaRepoMock = null!;
    private Mock<CategoriaFinanceiraPadraoSistemaQueryRepository> _queryMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private CriarCategoriaFinanceiraPadraoSistemaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _repoMock = new Mock<ICategoriaFinanceiraPadraoSistemaRepository>();
        _categoriaRepoMock = new Mock<ICategoriaFinanceiraRepository>();
        _queryMock = new Mock<CategoriaFinanceiraPadraoSistemaQueryRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new CriarCategoriaFinanceiraPadraoSistemaCommandHandler(
            _repoMock.Object,
            _categoriaRepoMock.Object,
            _queryMock.Object,
            _audit,
            _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private CriarCategoriaFinanceiraPadraoSistemaCommand ComandoPadrao(string nome = "Telemedicina")
        => new(nome, TipoCategoria.Receita, AdminId);

    // --- caminho feliz ---

    [Test]
    public async Task Handle_CaminhoFeliz_CriaGlobalEPropagaParaEstabelecimentos_CA4()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteGlobalComNomeETipo("Telemedicina", TipoCategoria.Receita, default))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.Salvar(It.IsAny<CategoriaFinanceiraPadraoSistema>(), default))
            .Returns(Task.CompletedTask);

        // 2 estabelecimentos, nenhum com o nome "Telemedicina"
        _queryMock.Setup(q => q.ListarIdsEstabelecimentos(default))
            .ReturnsAsync(new List<long> { 1L, 2L });
        _queryMock.Setup(q => q.ListarEstabelecimentosComNome("Telemedicina", default))
            .ReturnsAsync(new List<long>());

        // Act
        await _handler.Handle(ComandoPadrao());

        // Assert: global criado
        _repoMock.Verify(r => r.Salvar(It.IsAny<CategoriaFinanceiraPadraoSistema>(), default), Times.Once);

        // 2 cópias adicionadas ao DbSet (via _db.CategoriasFinanceiras.Add)
        Assert.That(_db.CategoriasFinanceiras.Local.Count, Is.EqualTo(2));
        Assert.That(_db.CategoriasFinanceiras.Local.All(c => c.Padrao), Is.True);
        Assert.That(_db.CategoriasFinanceiras.Local.All(c => c.Ativo), Is.True);
        Assert.That(_db.CategoriasFinanceiras.Local.All(c => c.Nome == "Telemedicina"), Is.True);

        // Audit gravado
        var audit = _db.ImedtoAdminAuditLogs.Local.Single();
        Assert.That(audit.Acao, Is.EqualTo("CRIAR_CATEGORIA_FINANCEIRA_PADRAO_SISTEMA"));
        Assert.That(audit.PayloadJson, Does.Contain("Telemedicina"));
        Assert.That(audit.PayloadJson, Does.Contain("nInstanciasPropagadas"));
    }

    [Test]
    public async Task Handle_PropagacaoIdempotente_PulaEstabelecimentosComNome_CA4()
    {
        // Arrange: estab 1 já tem "Telemedicina"; estab 2 não tem
        _repoMock.Setup(r => r.ExisteGlobalComNomeETipo("Telemedicina", TipoCategoria.Receita, default))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.Salvar(It.IsAny<CategoriaFinanceiraPadraoSistema>(), default))
            .Returns(Task.CompletedTask);

        _queryMock.Setup(q => q.ListarIdsEstabelecimentos(default))
            .ReturnsAsync(new List<long> { 1L, 2L });
        _queryMock.Setup(q => q.ListarEstabelecimentosComNome("Telemedicina", default))
            .ReturnsAsync(new List<long> { 1L }); // estab 1 já tem o nome

        // Act
        await _handler.Handle(ComandoPadrao());

        // Assert: apenas 1 cópia propagada (estab 2), estab 1 pulado
        Assert.That(_db.CategoriasFinanceiras.Local.Count, Is.EqualTo(1));
        Assert.That(_db.CategoriasFinanceiras.Local.Single().EstabelecimentoId, Is.EqualTo(2L));
    }

    // --- unicidade ---

    [Test]
    public void Handle_NomeDuplicadoGlobal_LancaBusinessException()
    {
        _repoMock.Setup(r => r.ExisteGlobalComNomeETipo("Consultas", TipoCategoria.Receita, default))
            .ReturnsAsync(true);

        var ex = Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new CriarCategoriaFinanceiraPadraoSistemaCommand("Consultas", TipoCategoria.Receita, AdminId)));

        Assert.That(ex!.Message, Does.Contain("categoria padrão"));
    }
}
