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
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Application.Admin.ModelosPermissaoPadraoSistema;

/// <summary>
/// Testes do handler de criar modelo de permissão padrão do sistema.
/// CA1, CA13 do briefing 2026-06-04_001.
/// </summary>
[TestFixture]
public class CriarModeloPermissaoPadraoSistemaHandlerTests
{
    private static readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext _db = null!;
    private Mock<IModeloPermissaoRepository> _repoMock = null!;
    private Mock<ModeloPermissaoPadraoSistemaQueryRepository> _queryMock = null!;
    private ImedtoAdminAuditWriter _audit = null!;
    private CriarModeloPermissaoPadraoSistemaCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _repoMock = new Mock<IModeloPermissaoRepository>();
        _queryMock = new Mock<ModeloPermissaoPadraoSistemaQueryRepository>();

        var httpMock = new Mock<IHttpContextAccessor>();
        httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _audit = new ImedtoAdminAuditWriter(
            _db,
            httpMock.Object,
            NullLogger<ImedtoAdminAuditWriter>.Instance);

        _handler = new CriarModeloPermissaoPadraoSistemaCommandHandler(
            _repoMock.Object, _queryMock.Object, _audit, _db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_CaminhoFeliz_CriaGlobalEPropagaParaEstabelecimentos()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteGlobalComNome("Financeiro", null, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExisteNomeEmQualquerEstabelecimento("Financeiro", null, default)).ReturnsAsync(false);

        // Simula 2 estabelecimentos existentes
        _queryMock.Setup(q => q.ListarIdsEstabelecimentos(default))
            .ReturnsAsync(new List<long> { 1L, 2L });

        _repoMock.Setup(r => r.Salvar(It.IsAny<ModeloPermissaoEstabelecimento>()))
            .Returns(Task.CompletedTask);

        var command = new CriarModeloPermissaoPadraoSistemaCommand(
            "Financeiro",
            TipoAcessoModelo.Recepcionista,
            new[] { "financeiro.ver", "financeiro.lancar" },
            null, "fa-sack-dollar", "hsl(40 80% 50%)", "Financeiro",
            _adminId);

        // Act — sem exception = sucesso
        var id = await _handler.Handle(command);

        // Assert
        // Salvar foi chamado 1x para o global
        _repoMock.Verify(r => r.Salvar(It.Is<ModeloPermissaoEstabelecimento>(m => m.EstabelecimentoId == null)), Times.Once);
        // Audit gravado
        var audit = _db.ImedtoAdminAuditLogs.Single();
        Assert.That(audit.Acao, Is.EqualTo("CRIAR_MODELO_PERMISSAO_PADRAO_SISTEMA"));
        Assert.That(audit.RecursoTipo, Is.EqualTo("modelo_permissao_padrao"));
        Assert.That(audit.PayloadJson, Does.Contain("Financeiro"));
        Assert.That(audit.PayloadJson, Does.Contain("nInstanciasPropagadas"));
    }

    [Test]
    public void Handle_NomeDuplicadoGlobal_Lanca422()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteGlobalComNome("Admin", null, default)).ReturnsAsync(true);

        var command = new CriarModeloPermissaoPadraoSistemaCommand(
            "Admin", TipoAcessoModelo.Profissional, null, null, null, null, null, _adminId);

        // Act + Assert
        Assert.ThrowsAsync<BusinessException>(async () => await _handler.Handle(command));
    }

    [Test]
    public void Handle_NomeExisteEmEstabelecimento_Lanca422()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteGlobalComNome("Médico", null, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExisteNomeEmQualquerEstabelecimento("Médico", null, default)).ReturnsAsync(true);

        var command = new CriarModeloPermissaoPadraoSistemaCommand(
            "Médico", TipoAcessoModelo.Profissional, null, null, null, null, null, _adminId);

        // Act + Assert
        Assert.ThrowsAsync<BusinessException>(async () => await _handler.Handle(command));
    }
}
