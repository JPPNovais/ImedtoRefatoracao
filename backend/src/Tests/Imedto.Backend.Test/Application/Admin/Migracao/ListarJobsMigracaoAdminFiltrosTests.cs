using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Admin.Migracao;

/// <summary>
/// Verifica que os novos filtros (CriadoDe, CriadoAte, Onda, Origem) são repassados
/// ao repositório pelo ListarJobsMigracaoAdminQueryHandler (addendum 003 — CA51-CA69).
/// </summary>
[TestFixture]
public class ListarJobsMigracaoAdminFiltrosTests
{
    private Mock<MigracaoAdminQueryRepository> _repo;
    private ListarJobsMigracaoAdminQueryHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<MigracaoAdminQueryRepository>(new AppReadConnectionString("Host=fake")) { CallBase = false };
        _repo
            .Setup(r => r.ListarJobsAsync(
                It.IsAny<long?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<MigracaoJobAdminDto>(), 0));

        _sut = new ListarJobsMigracaoAdminQueryHandler(_repo.Object);
    }

    [Test]
    public async Task Handle_RepassaCriadoDeAoRepositorio()
    {
        var de = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _sut.Handle(new ListarJobsMigracaoAdminQuery { CriadoDe = de });

        _repo.Verify(r => r.ListarJobsAsync(
            It.IsAny<long?>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(),
            de, It.IsAny<DateTime?>(),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_RepassaCriadoAteAoRepositorio()
    {
        var ate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        await _sut.Handle(new ListarJobsMigracaoAdminQuery { CriadoAte = ate });

        _repo.Verify(r => r.ListarJobsAsync(
            It.IsAny<long?>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<DateTime?>(), ate,
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_RepassaOndaAoRepositorio()
    {
        await _sut.Handle(new ListarJobsMigracaoAdminQuery { Onda = "onda1" });

        _repo.Verify(r => r.ListarJobsAsync(
            It.IsAny<long?>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            "onda1", It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_RepassaOrigemAoRepositorio()
    {
        await _sut.Handle(new ListarJobsMigracaoAdminQuery { Origem = "iClinic" });

        _repo.Verify(r => r.ListarJobsAsync(
            It.IsAny<long?>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string?>(), "iClinic",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_TodosFiltrosNulos_NaoQuebra()
    {
        var resultado = await _sut.Handle(new ListarJobsMigracaoAdminQuery());

        Assert.That(resultado.Total, Is.EqualTo(0));
        Assert.That(resultado.Itens, Is.Empty);
    }
}
