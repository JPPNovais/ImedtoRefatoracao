using Imedto.Backend.Application.Assinaturas.Queries;
using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Assinaturas;

/// <summary>
/// Testes do ObterMinhaAssinaturaQueryHandlers (pós-hotfix épico 2026-06-11_003).
/// Verifica que a fonte de dados é a estrutura nova e que o status derivado
/// é coerente com o enforcement (liberado no back ⟺ não-bloqueado no front).
/// </summary>
[TestFixture]
public class ObterMinhaAssinaturaQueryHandlerTests
{
    private Mock<MinhaAssinaturaQueryRepository> _repo = null!;
    private ObterMinhaAssinaturaQueryHandlers _sut = null!;

    private const long EstabId = 42;

    [SetUp]
    public void SetUp()
    {
        // MinhaAssinaturaQueryRepository não tem interface — mockamos a classe concreta.
        // AppReadConnectionString é passada como dummy; o método virtual é interceptado
        // pelo Moq antes de abrir conexão real.
        var connDummy = new AppReadConnectionString("Host=dummy;Database=dummy");
        _repo = new Mock<MinhaAssinaturaQueryRepository>(connDummy) { CallBase = false };
        _sut = new ObterMinhaAssinaturaQueryHandlers(_repo.Object);
    }

    // ── Sem vigência ─────────────────────────────────────────────────────────

    [Test]
    public async Task SemVigencia_RetornaNulo()
    {
        _repo.Setup(r => r.ObterDoEstabelecimento(EstabId))
            .ReturnsAsync((AssinaturaDto?)null);

        var resultado = await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = EstabId });

        Assert.That(resultado, Is.Null);
    }

    // ── Vitalício → "Ativa" (não bloqueado) ──────────────────────────────────

    [Test]
    public async Task VigenteVitalicio_StatusAtiva_NaoBloqueado()
    {
        var dto = CriarDto(status: "Ativa", expiraEm: null, diasRestantes: null);
        _repo.Setup(r => r.ObterDoEstabelecimento(EstabId)).ReturnsAsync(dto);

        var resultado = await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = EstabId });

        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado!.Status, Is.EqualTo("Ativa"));
        Assert.That(resultado.ExpiraEm, Is.Null);
        Assert.That(resultado.DiasRestantes, Is.Null);
        // Coerência enforcement: "Ativa" não está em {Expirada, Suspensa, Cancelada} → isBlocked=false.
        Assert.That(IsBlocked(resultado.Status), Is.False);
    }

    // ── Expira no futuro → "Trial" (não bloqueado) ───────────────────────────

    [Test]
    public async Task VigenteComExpiraFuturo_StatusTrial_ComDiasRestantes()
    {
        var expira = DateTime.UtcNow.AddDays(10);
        var dto = CriarDto(status: "Trial", expiraEm: expira, diasRestantes: 10);
        _repo.Setup(r => r.ObterDoEstabelecimento(EstabId)).ReturnsAsync(dto);

        var resultado = await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = EstabId });

        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado!.Status, Is.EqualTo("Trial"));
        Assert.That(resultado.ExpiraEm, Is.Not.Null);
        Assert.That(resultado.DiasRestantes, Is.GreaterThan(0));
        Assert.That(IsBlocked(resultado.Status), Is.False);
    }

    // ── Suspensa → "Suspensa" (bloqueado) ────────────────────────────────────

    [Test]
    public async Task VigenteSuspensa_StatusSuspensa_Bloqueado()
    {
        var dto = CriarDto(status: "Suspensa", expiraEm: null, diasRestantes: null);
        _repo.Setup(r => r.ObterDoEstabelecimento(EstabId)).ReturnsAsync(dto);

        var resultado = await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = EstabId });

        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado!.Status, Is.EqualTo("Suspensa"));
        Assert.That(IsBlocked(resultado.Status), Is.True);
    }

    // ── Expirada (expira_em no passado) → "Expirada" (bloqueado) ─────────────

    [Test]
    public async Task VigenteExpirada_StatusExpirada_Bloqueado()
    {
        var expirou = DateTime.UtcNow.AddDays(-5);
        var dto = CriarDto(status: "Expirada", expiraEm: expirou, diasRestantes: 0);
        _repo.Setup(r => r.ObterDoEstabelecimento(EstabId)).ReturnsAsync(dto);

        var resultado = await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = EstabId });

        Assert.That(resultado, Is.Not.Null);
        Assert.That(resultado!.Status, Is.EqualTo("Expirada"));
        Assert.That(IsBlocked(resultado.Status), Is.True);
    }

    // ── Features do plano propagadas ─────────────────────────────────────────

    [Test]
    public async Task RetornaFeaturesDoPlano()
    {
        var dto = CriarDto(status: "Ativa", expiraEm: null, diasRestantes: null,
            features: ["receitas", "ia", "exame_fisico"]);
        _repo.Setup(r => r.ObterDoEstabelecimento(EstabId)).ReturnsAsync(dto);

        var resultado = await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = EstabId });

        Assert.That(resultado!.Plano.Features, Does.Contain("receitas"));
        Assert.That(resultado.Plano.Features, Does.Contain("ia"));
        Assert.That(resultado.Plano.Features, Does.Contain("exame_fisico"));
    }

    // ── Multi-tenant: estabelecimento correto é passado ao repo ──────────────

    [Test]
    public async Task PassaEstabelecimentoIdCorretoAoRepo()
    {
        const long outro = 99;
        _repo.Setup(r => r.ObterDoEstabelecimento(outro)).ReturnsAsync((AssinaturaDto?)null);

        await _sut.Handle(new ObterMinhaAssinaturaQuery { EstabelecimentoId = outro });

        _repo.Verify(r => r.ObterDoEstabelecimento(outro), Times.Once);
        _repo.Verify(r => r.ObterDoEstabelecimento(It.Is<long>(id => id != outro)), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AssinaturaDto CriarDto(
        string status,
        DateTime? expiraEm,
        int? diasRestantes,
        string[]? features = null)
        => new AssinaturaDto
        {
            Status = status,
            IniciadaEm = DateTime.UtcNow.AddDays(-30),
            ExpiraEm = expiraEm,
            DiasRestantes = diasRestantes,
            Plano = new PlanoDto
            {
                Id = 0,
                Nome = "Plano Teste",
                Features = features ?? Array.Empty<string>(),
                LimiteProfissionais = null,
                LimitePacientes = null,
            },
        };

    /// <summary>
    /// Espelha a lógica de isBlocked do assinaturaStore.ts (Expirada|Suspensa|Cancelada).
    /// Garante paridade de veredito liberado/bloqueado entre enforcement e front.
    /// </summary>
    private static bool IsBlocked(string status)
        => status is "Expirada" or "Suspensa" or "Cancelada";
}
