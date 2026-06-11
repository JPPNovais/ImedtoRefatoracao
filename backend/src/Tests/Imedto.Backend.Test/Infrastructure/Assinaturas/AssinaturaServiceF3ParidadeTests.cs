using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Assinaturas;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Assinaturas;

/// <summary>
/// Testes de paridade comportamental do AssinaturaService após a F3 (briefing 2026-06-11_003).
/// Cobre CA16–CA22: o enforcement agora lê a estrutura nova (imedto_assinaturas/imedto_planos)
/// e entrega os MESMOS contratos de retorno (402 AssinaturaInativa / FeatureBloqueada / limites)
/// que a estrutura legada entregava.
/// </summary>
[TestFixture]
public class AssinaturaServiceF3ParidadeTests : IDisposable
{
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepo;
    private Mock<IImedtoPlanoRepository> _planoRepo;
    private MemoryCache _cache;
    private Mock<ILogger<AssinaturaService>> _logger;
    private AssinaturaService _sut;

    private static readonly Guid _planoId = Guid.NewGuid();
    private static readonly Guid _adminId = Guid.NewGuid();
    private const long _estId = 42;

    public void Dispose()
    {
        _cache?.Dispose();
        GC.SuppressFinalize(this);
    }

    [SetUp]
    public void SetUp()
    {
        _assinaturaRepo = new Mock<IImedtoAssinaturaRepository>();
        _planoRepo = new Mock<IImedtoPlanoRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = new Mock<ILogger<AssinaturaService>>();

        // Connection string inválida — testes que chegam à query de contagem devem ser integração.
        _sut = new AssinaturaService(
            _assinaturaRepo.Object,
            _planoRepo.Object,
            _cache,
            _logger.Object,
            new AppReadConnectionString("Host=invalid;Database=x"));
    }

    // --------------------------------------------------
    // CA16 — Assinatura inativa → 402 AssinaturaInativa
    // --------------------------------------------------

    [Test]
    public async Task TenantEstaAtivo_SemVigenciaAtiva_RetornaFalse_CA16()
    {
        // Sem vigência cadastrada (null) → BLOQUEADO.
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var resultado = await _sut.TenantEstaAtivo(_estId);

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task AvaliarFeature_SemVigencia_RetornaAssinaturaInativa_CA16()
    {
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var resultado = await _sut.AvaliarFeature(_estId, Features.Ia);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.AssinaturaInativa));
    }

    // --------------------------------------------------
    // CA17 — Liberado (vitalício) → sem 402
    // --------------------------------------------------

    [Test]
    public async Task TenantEstaAtivo_VigenteVitalicio_RetornaTrue_CA17()
    {
        var assinatura = ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId); // sem expiraEm = vitalício
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var resultado = await _sut.TenantEstaAtivo(_estId);

        Assert.That(resultado, Is.True);
    }

    [Test]
    public async Task TenantEstaAtivo_VigenteComExpiraEmFuturo_RetornaTrue_CA17()
    {
        var assinatura = ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(30));
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var resultado = await _sut.TenantEstaAtivo(_estId);

        Assert.That(resultado, Is.True);
    }

    // --------------------------------------------------
    // CA18 — Feature bloqueada (plano sem a feature) → FeatureNaoIncluida
    // --------------------------------------------------

    [Test]
    public async Task AvaliarFeature_PlanoSemFeatureIa_RetornaFeatureNaoIncluida_CA18()
    {
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComFeatures("""{"receitas":true,"ia":false}""");

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var resultado = await _sut.AvaliarFeature(_estId, Features.Ia);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.FeatureNaoIncluida));
    }

    [Test]
    public async Task AvaliarFeature_PlanoComFeatureIa_RetornaLiberada_CA18()
    {
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComFeatures("""{"receitas":true,"ia":true}""");

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var resultado = await _sut.AvaliarFeature(_estId, Features.Ia);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.Liberada));
    }

    [TestCase(Features.Receitas)]
    [TestCase(Features.ExameFisico)]
    [TestCase(Features.ProcedimentosCirurgicos)]
    [TestCase(Features.OrcamentoCompleto)]
    [TestCase(Features.Ia)]
    [TestCase(Features.RelatoriosAvancados)]
    public async Task AvaliarFeature_PlanoSemQualquerFeatureGateada_RetornaFeatureNaoIncluida_CA18(string feature)
    {
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComFeatures("{}"); // plano sem nenhuma feature

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var resultado = await _sut.AvaliarFeature(_estId, feature);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.FeatureNaoIncluida));
    }

    // --------------------------------------------------
    // CA19 — Limite de paciente (plano com limite)
    // --------------------------------------------------

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimitePacientesNulo_RetornaFalse_CA19()
    {
        // Plano ilimitado (pacientes null) → nunca atinge.
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComLimites("""{"profissionais":null,"pacientes":null}""");

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var resultado = await _sut.LimiteAtingidoAsync(_estId, "pacientes");

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimitesJsonVazio_RetornaFalse_CA19()
    {
        // LimitesJson "{}" = ilimitado.
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComLimites("{}");

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var resultado = await _sut.LimiteAtingidoAsync(_estId, "pacientes");

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task LimiteAtingidoAsync_SemAssinatura_RetornaTrue_CA19()
    {
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var resultado = await _sut.LimiteAtingidoAsync(_estId, "pacientes");

        Assert.That(resultado, Is.True);
    }

    [Test]
    public async Task LimiteAtingidoAsync_SemPlano_RetornaTrue_CA19()
    {
        var assinatura = AssinaturaVigenteVitalicia();

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoPlano?)null);

        var resultado = await _sut.LimiteAtingidoAsync(_estId, "pacientes");

        Assert.That(resultado, Is.True);
    }

    // --------------------------------------------------
    // CA20 — Limite de profissional (plano com limite)
    // --------------------------------------------------

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimiteProfissionaisNulo_RetornaFalse_CA20()
    {
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComLimites("{}");

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var resultado = await _sut.LimiteAtingidoAsync(_estId, "profissionais");

        Assert.That(resultado, Is.False);
    }

    // --------------------------------------------------
    // CA21 — Suspensão manual bloqueia mesmo com expiraEm futuro
    // --------------------------------------------------

    [Test]
    public async Task TenantEstaAtivo_SuspensaComExpiraEmFuturo_RetornaFalse_CA21()
    {
        var assinatura = ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(30));
        assinatura.Suspender(); // suspensão manual vence a vigência ativa

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var resultado = await _sut.TenantEstaAtivo(_estId);

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task AvaliarFeature_AssinaturaSuspensa_RetornaAssinaturaInativa_CA21()
    {
        var assinatura = ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(30));
        assinatura.Suspender();

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var resultado = await _sut.AvaliarFeature(_estId, Features.Ia);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.AssinaturaInativa));
    }

    // --------------------------------------------------
    // CA22 — Trial expirado (expiraEm no passado) bloqueia
    // --------------------------------------------------

    [Test]
    public async Task TenantEstaAtivo_ExpiraEmPassado_RetornaFalse_CA22()
    {
        // Invariante do domínio impede criar com expiraEm no passado via .Criar().
        // Usamos reflexão para setar ExpiraEm no passado em uma instância criada normalmente.
        var assinatura = ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(1)); // futuro para passar a invariante
        SetExpiraEmNaPassado(assinatura); // retroceção após criação para simular expiração

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var resultado = await _sut.TenantEstaAtivo(_estId);

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task AvaliarFeature_AssinaturaExpirada_RetornaAssinaturaInativa_CA22()
    {
        var assinatura = ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId,
            expiraEm: DateTimeOffset.UtcNow.AddDays(1));
        SetExpiraEmNaPassado(assinatura);

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        var resultado = await _sut.AvaliarFeature(_estId, Features.Ia);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.AssinaturaInativa));
    }

    /// <summary>
    /// Manipula ExpiraEm via reflexão para simular expiração natural (estado que existe no banco
    /// mas não pode ser criado pelo Domain para evitar invariante).
    /// </summary>
    private static void SetExpiraEmNaPassado(ImedtoAssinatura assinatura)
    {
        // ImedtoAssinatura.ExpiraEm é virtual/protected set — acessível por reflexão.
        var prop = typeof(ImedtoAssinatura).GetProperty("ExpiraEm");
        prop!.SetValue(assinatura, DateTimeOffset.UtcNow.AddDays(-1));
    }

    // --------------------------------------------------
    // Fail-closed: estabelecimentoId inválido sempre bloqueia
    // --------------------------------------------------

    [Test]
    public async Task TenantEstaAtivo_EstabelecimentoInvalido_RetornaFalse()
    {
        var resultado = await _sut.TenantEstaAtivo(0);

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task AvaliarFeature_EstabelecimentoInvalido_RetornaAssinaturaInativa()
    {
        var resultado = await _sut.AvaliarFeature(0, Features.Ia);

        Assert.That(resultado, Is.EqualTo(ResultadoFeature.AssinaturaInativa));
    }

    [Test]
    public async Task LimiteAtingidoAsync_EstabelecimentoInvalido_RetornaTrue()
    {
        var resultado = await _sut.LimiteAtingidoAsync(0, "profissionais");

        Assert.That(resultado, Is.True);
    }

    // --------------------------------------------------
    // Recurso desconhecido ainda lança
    // --------------------------------------------------

    [Test]
    public void LimiteAtingidoAsync_RecursoDesconhecido_LancaArgumentException()
    {
        var assinatura = AssinaturaVigenteVitalicia();
        var plano = PlanoComLimites("""{"profissionais":5}""");

        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo.Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.LimiteAtingidoAsync(_estId, "desconhecido"));

        Assert.That(ex!.Message, Does.Contain("Recurso desconhecido"));
    }

    // --------------------------------------------------
    // Cache de 1min é mantido (InvalidarCache reseta as chaves)
    // --------------------------------------------------

    [Test]
    public async Task InvalidarCache_LimpaTodasChaves_ProximaConsultaBateNoRepo()
    {
        var assinatura = AssinaturaVigenteVitalicia();
        _assinaturaRepo.Setup(r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);

        // 1ª consulta: popula cache.
        await _sut.TenantEstaAtivo(_estId);

        // Invalida o cache.
        _sut.InvalidarCache(_estId);

        // 2ª consulta: deve bater no repo de novo.
        await _sut.TenantEstaAtivo(_estId);

        _assinaturaRepo.Verify(
            r => r.ObterVigenteDoEstabelecimentoAsync(_estId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------

    private ImedtoAssinatura AssinaturaVigenteVitalicia()
        => ImedtoAssinatura.Criar(_estId, _planoId, false, null, _adminId);

    private ImedtoPlano PlanoComFeatures(string featuresJson)
        => ImedtoPlano.Criar("Plano Teste", null, null, false, "{}", _adminId, featuresJson: featuresJson);

    private ImedtoPlano PlanoComLimites(string limitesJson)
        => ImedtoPlano.Criar("Plano Teste", null, null, false, limitesJson, _adminId);
}
