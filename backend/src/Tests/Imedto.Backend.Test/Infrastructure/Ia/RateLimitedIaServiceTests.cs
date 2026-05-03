using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Infrastructure.Ia;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Ia;

[TestFixture]
public class RateLimitedIaServiceTests
{
    private Mock<IIaService> _inner;
    private Mock<IAiAuditRepository> _audit;
    private Mock<IAiCacheRepository> _cache;
    private Mock<IAiRateLimitRepository> _rate;
    private Mock<IEstabelecimentoIaSettingsRepository> _settings;
    private Mock<IVinculoRepository> _vinculos;
    private Mock<IModeloPermissaoRepository> _modelosPermissao;
    private Mock<IHttpContextAccessor> _http;
    private IOptions<IaOptions> _opts;
    private IConfiguration _config;

    private readonly Guid _usuarioId = Guid.NewGuid();
    private const long EstabelecimentoId = 5;

    [SetUp]
    public void SetUp()
    {
        _inner = new Mock<IIaService>();
        _audit = new Mock<IAiAuditRepository>();
        _cache = new Mock<IAiCacheRepository>();
        _rate = new Mock<IAiRateLimitRepository>();
        _settings = new Mock<IEstabelecimentoIaSettingsRepository>();
        _vinculos = new Mock<IVinculoRepository>();
        _modelosPermissao = new Mock<IModeloPermissaoRepository>();
        _http = new Mock<IHttpContextAccessor>();

        // Default: tenant não configurou IA settings — decorator usa defaults globais.
        _settings
            .Setup(s => s.ObterPorEstabelecimentoOuNulo(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EstabelecimentoIaSettings)null);

        _opts = Options.Create(new IaOptions { LimitePorMinuto = 10, CacheTtlHoras = 24 });
        _config = new ConfigurationBuilder().Build();

        ConfigurarHttpContext();

        // Default: usuário tem permissão para usar IA neste tenant. Os testes que
        // exercitam negação explícita reconfiguram este mock no Arrange.
        _vinculos
            .Setup(v => v.PodeAtuarComoProfissional(It.IsAny<Guid>(), It.IsAny<long>()))
            .ReturnsAsync(true);

        // Default: modelo do vínculo concede a permissão fina de assistente clínico de IA.
        // Testes que negam permissão fina reconfiguram esse mock no Arrange.
        _modelosPermissao
            .Setup(m => m.UsuarioTemPermissaoExtra(
                It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _audit
            .Setup(a => a.RegistrarAsync(It.IsAny<AiAuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cache
            .Setup(c => c.SalvarAsync(
                It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void ConfigurarHttpContext()
    {
        var claims = new[]
        {
            new Claim("sub", _usuarioId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var headers = new HeaderDictionary
        {
            { "X-Estabelecimento-Id", EstabelecimentoId.ToString() }
        };

        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Headers).Returns(headers);

        var ctx = new Mock<HttpContext>();
        ctx.Setup(c => c.User).Returns(principal);
        ctx.Setup(c => c.Request).Returns(request.Object);

        _http.Setup(h => h.HttpContext).Returns(ctx.Object);
    }

    private RateLimitedIaService CriarSut() =>
        new(_inner.Object, _audit.Object, _cache.Object, _rate.Object, _settings.Object, _vinculos.Object, _modelosPermissao.Object, _http.Object, _opts, _config);

    private static SugestaoSecaoProntuarioRequest CriarRequest(string secao = "Queixa principal") =>
        new() { SecaoAlvoTitulo = secao, SecoesContexto = new Dictionary<string, string>() };

    private static async IAsyncEnumerable<string> ChunksOf(params string[] chunks)
    {
        foreach (var c in chunks)
        {
            yield return c;
            await Task.Yield();
        }
    }

    private static string Sha256Hex(string entrada)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(entrada));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // 1) Cache hit

    [Test]
    public async Task SugerirSecaoProntuarioAsync_CacheHit_NaoChamaInner_RetornaConteudoCached()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        const string outputCached = "Sugestão em cache";
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputCached);

        var sut = CriarSut();
        var req = CriarRequest();

        // Act
        var resultados = new List<string>();
        await foreach (var chunk in sut.SugerirSecaoProntuarioAsync(req))
            resultados.Add(chunk);

        // Assert
        Assert.That(resultados, Is.EqualTo(new[] { outputCached }));
        _inner.Verify(i => i.SugerirSecaoProntuarioAsync(It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SugerirSecaoProntuarioAsync_CacheHit_RegistraAuditComEndpointCache()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resposta cached");

        var sut = CriarSut();

        // Act
        await foreach (var _ in sut.SugerirSecaoProntuarioAsync(CriarRequest())) { }

        // Assert — audit com endpoint = "sugestao-secao-cache"
        _audit.Verify(a => a.RegistrarAsync(
            It.Is<AiAuditLog>(log =>
                log.Endpoint == "sugestao-secao-cache" &&
                log.UsuarioId == _usuarioId &&
                log.EstabelecimentoId == EstabelecimentoId &&
                log.Sucesso == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // 1.5) Permissão fina (item 3.4): vínculo ativo mas modelo sem `ia_assistente_clinico`
    [Test]
    public void SugerirSecaoProntuarioAsync_SemPermissaoFinaIa_LancaBusinessException()
    {
        // Arrange — pode atuar mas modelo de permissão NÃO concede assistente de IA.
        _modelosPermissao
            .Setup(m => m.UsuarioTemPermissaoExtra(
                _usuarioId, EstabelecimentoId, PermissoesExtras.AssistenteClinicoIa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CriarSut();

        // Act + Assert
        Assert.ThrowsAsync<BusinessException>(async () =>
        {
            await foreach (var _ in sut.SugerirSecaoProntuarioAsync(CriarRequest())) { }
        });

        // Não deve sequer chegar a registrar tentativa de rate limit (gate é antes).
        _rate.Verify(r => r.RegistrarTentativaAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _inner.Verify(i => i.SugerirSecaoProntuarioAsync(
            It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // 2) Rate limit excedido

    [Test]
    public void SugerirSecaoProntuarioAsync_RateLimitExcedido_LancaBusinessException()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CriarSut();

        // Act + Assert — consumir o IAsyncEnumerable para forçar execução do método
        Assert.ThrowsAsync<BusinessException>(async () =>
        {
            await foreach (var _ in sut.SugerirSecaoProntuarioAsync(CriarRequest())) { }
        });
    }

    [Test]
    public async Task SugerirSecaoProntuarioAsync_RateLimitExcedido_NaoChamaInner()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CriarSut();

        // Act
        try
        {
            await foreach (var _ in sut.SugerirSecaoProntuarioAsync(CriarRequest())) { }
        }
        catch (BusinessException) { }

        // Assert
        _inner.Verify(i => i.SugerirSecaoProntuarioAsync(
            It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // 3) Sucesso de stream

    [Test]
    public async Task SugerirSecaoProntuarioAsync_Sucesso_EntregaChunksEGravaCacheEAudit()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var chunks = new[] { "Olá ", "mundo", "!" };
        _inner.Setup(i => i.SugerirSecaoProntuarioAsync(It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ChunksOf(chunks));

        var sut = CriarSut();

        // Act
        var resultados = new List<string>();
        await foreach (var c in sut.SugerirSecaoProntuarioAsync(CriarRequest()))
            resultados.Add(c);

        // Assert — chunks entregues corretamente
        Assert.That(resultados, Is.EqualTo(chunks));

        // Cache salvo com a resposta acumulada
        _cache.Verify(c => c.SalvarAsync(
            It.IsAny<string>(),
            EstabelecimentoId,
            "sugestao-secao",
            "Olá mundo!",
            It.IsAny<DateTime>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);

        // Audit com sucesso = true
        _audit.Verify(a => a.RegistrarAsync(
            It.Is<AiAuditLog>(log => log.Sucesso && log.Endpoint == "sugestao-secao"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // 4) Erro durante stream

    [Test]
    public async Task SugerirSecaoProntuarioAsync_ErroNoInner_AuditRegistradoComSucessoFalse()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        _inner.Setup(i => i.SugerirSecaoProntuarioAsync(It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()))
            .Returns(LancarExcecao("Falha de comunicação com a IA"));

        var sut = CriarSut();

        // Act
        try
        {
            await foreach (var _ in sut.SugerirSecaoProntuarioAsync(CriarRequest())) { }
        }
        catch { }

        // Assert — audit com sucesso = false
        _audit.Verify(a => a.RegistrarAsync(
            It.Is<AiAuditLog>(log =>
                !log.Sucesso &&
                log.Endpoint == "sugestao-secao"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SugerirSecaoProntuarioAsync_ErroNoInner_NaoGravaCachee()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        _inner.Setup(i => i.SugerirSecaoProntuarioAsync(It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()))
            .Returns(LancarExcecao("Erro"));

        var sut = CriarSut();

        // Act
        try
        {
            await foreach (var _ in sut.SugerirSecaoProntuarioAsync(CriarRequest())) { }
        }
        catch { }

        // Assert — cache NÃO deve ser salvo em caso de erro
        _cache.Verify(c => c.SalvarAsync(
            It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // 5) Hash determinístico

    [Test]
    public async Task SugerirSecaoProntuarioAsync_MesmoRequest_GeraHashIdentico()
    {
        // Arrange — captura os promptHash passados ao cache em duas chamadas diferentes
        _rate.Setup(r => r.RegistrarTentativaAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var hashesCapturados = new List<string>();
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((h, _) => hashesCapturados.Add(h))
            .ReturnsAsync((string)null);

        _inner.Setup(i => i.SugerirSecaoProntuarioAsync(It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ChunksOf("ok"));

        var sut = CriarSut();
        var req = CriarRequest("Queixa");

        // Act — duas chamadas com o mesmo request
        await foreach (var _ in sut.SugerirSecaoProntuarioAsync(req)) { }
        await foreach (var _ in sut.SugerirSecaoProntuarioAsync(req)) { }

        // Assert — hash idêntico nas duas chamadas
        Assert.That(hashesCapturados, Has.Count.EqualTo(2));
        Assert.That(hashesCapturados[0], Is.EqualTo(hashesCapturados[1]));
    }

    // 6) Audit NÃO contém prompt cru

    [Test]
    public async Task SugerirSecaoProntuarioAsync_Sucesso_AuditNaoContemPromptCru()
    {
        // Arrange
        _rate.Setup(r => r.RegistrarTentativaAsync(_usuarioId, EstabelecimentoId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _cache.Setup(c => c.ObterAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);
        _inner.Setup(i => i.SugerirSecaoProntuarioAsync(It.IsAny<SugestaoSecaoProntuarioRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ChunksOf("resposta"));

        var sut = CriarSut();
        var req = CriarRequest("Queixa principal com dado sensível do paciente");

        AiAuditLog auditRegistrado = null;
        _audit.Setup(a => a.RegistrarAsync(It.IsAny<AiAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AiAuditLog, CancellationToken>((log, _) => auditRegistrado = log)
            .Returns(Task.CompletedTask);

        // Act
        await foreach (var _ in sut.SugerirSecaoProntuarioAsync(req)) { }

        // Assert — PromptHash deve ser hash hex (64 chars), não o prompt em texto cru
        Assert.That(auditRegistrado, Is.Not.Null);
        Assert.That(auditRegistrado.PromptHash, Has.Length.EqualTo(64));
        Assert.That(auditRegistrado.PromptHash, Does.Not.Contain("Queixa principal"));

        // PromptHash esperado: sha256 do JSON serializado do request
        var hashEsperado = Sha256Hex(JsonSerializer.Serialize(req));
        Assert.That(auditRegistrado.PromptHash, Is.EqualTo(hashEsperado));
    }

    // Auxiliar — gera IAsyncEnumerable que lança exceção
    private static async IAsyncEnumerable<string> LancarExcecao(
        string mensagem,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        // 'if (false) yield break' satisfaz o compilador de IAsyncEnumerable sem CS0162
        // (codigo inalcancavel). Mensagem.Length nunca eh < 0, mas o compilador nao prova.
        if (mensagem.Length < 0) yield break;
        throw new InvalidOperationException(mensagem);
    }
}
