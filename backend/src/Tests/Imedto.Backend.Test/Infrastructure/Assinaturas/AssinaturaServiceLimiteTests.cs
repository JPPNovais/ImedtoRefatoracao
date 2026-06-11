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
/// Testa LimiteAtingidoAsync em cenários offline (sem banco real).
/// ContarProfissionaisAtivos usa Dapper + NpgsqlConnection, então usa connection string inválida
/// para forçar o caminho onde o limite é nulo (ilimitado) — sem atingir o banco.
/// Os cenários que chegam à query SQL estão anotados abaixo.
///
/// Migrado na F3 (briefing 2026-06-11_003): mocks agora usam IImedtoAssinaturaRepository/
/// IImedtoPlanoRepository (estrutura nova) em vez das interfaces legadas.
/// </summary>
[TestFixture]
public class AssinaturaServiceLimiteTests : IDisposable
{
    private Mock<IImedtoAssinaturaRepository> _assinaturaRepo;
    private Mock<IImedtoPlanoRepository> _planoRepo;
    private MemoryCache _cache;
    private Mock<ILogger<AssinaturaService>> _logger;
    private AssinaturaService _sut;

    private static readonly Guid _planoId = Guid.NewGuid();
    private static readonly Guid _adminId = Guid.NewGuid();

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

        // Connection string inválida — só chega a ela quando o limite não é nulo.
        // Testes que chegam até a query devem ser escritos como integração.
        _sut = new AssinaturaService(
            _assinaturaRepo.Object,
            _planoRepo.Object,
            _cache,
            _logger.Object,
            new AppReadConnectionString("Host=invalid;Database=x"));
    }

    [Test]
    public async Task LimiteAtingidoAsync_SemAssinatura_RetornaTrue()
    {
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoAssinatura?)null);

        var resultado = await _sut.LimiteAtingidoAsync(1, "profissionais");

        Assert.That(resultado, Is.True);
    }

    [Test]
    public async Task LimiteAtingidoAsync_SemPlano_RetornaTrue()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImedtoPlano?)null);

        var resultado = await _sut.LimiteAtingidoAsync(1, "profissionais");

        Assert.That(resultado, Is.True);
    }

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimiteProfissionaisNulo_RetornaFalse()
    {
        // Plano ilimitado (profissionais ausente no JSON) deve retornar false sem consultar o banco.
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        var planoIlimitado = ImedtoPlano.Criar("Enterprise", null, null, false, "{}", _adminId);

        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoIlimitado);

        var resultado = await _sut.LimiteAtingidoAsync(1, "profissionais");

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimitePacientesNulo_RetornaFalse()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        var planoIlimitado = ImedtoPlano.Criar("Enterprise", null, null, false, "{}", _adminId);

        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planoIlimitado);

        var resultado = await _sut.LimiteAtingidoAsync(1, "pacientes");

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task LimiteAtingidoAsync_EstabelecimentoInvalido_RetornaTrue()
    {
        // Política fail-closed: estabelecimento inválido = limite atingido
        var resultado = await _sut.LimiteAtingidoAsync(0, "profissionais");

        Assert.That(resultado, Is.True);
    }

    [Test]
    public void LimiteAtingidoAsync_RecursoDesconhecido_LancaArgumentException()
    {
        var assinatura = ImedtoAssinatura.Criar(1, _planoId, false, null, _adminId);
        var plano = ImedtoPlano.Criar("Basico", null, 4900, false, """{"profissionais":2,"pacientes":50}""", _adminId);

        _assinaturaRepo
            .Setup(r => r.ObterVigenteDoEstabelecimentoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdAsync(_planoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.LimiteAtingidoAsync(1, "desconhecido"));

        Assert.That(ex!.Message, Does.Contain("Recurso desconhecido"));
    }
}
