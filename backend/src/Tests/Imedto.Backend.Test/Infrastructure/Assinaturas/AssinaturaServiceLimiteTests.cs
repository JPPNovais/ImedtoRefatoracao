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
/// </summary>
[TestFixture]
public class AssinaturaServiceLimiteTests
{
    private Mock<IAssinaturaRepository> _assinaturaRepo;
    private Mock<IPlanoRepository> _planoRepo;
    private IMemoryCache _cache;
    private Mock<ILogger<AssinaturaService>> _logger;
    private AssinaturaService _sut;

    [SetUp]
    public void SetUp()
    {
        _assinaturaRepo = new Mock<IAssinaturaRepository>();
        _planoRepo = new Mock<IPlanoRepository>();
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
            .Setup(r => r.ObterPorEstabelecimentoOuNulo(1))
            .ReturnsAsync((Assinatura?)null);

        var resultado = await _sut.LimiteAtingidoAsync(1, "profissionais");

        Assert.That(resultado, Is.True);
    }

    [Test]
    public async Task LimiteAtingidoAsync_SemPlano_RetornaTrue()
    {
        var assinatura = Assinatura.IniciarTrial(1, 99, TimeSpan.FromDays(14));
        _assinaturaRepo
            .Setup(r => r.ObterPorEstabelecimentoOuNulo(1))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdOuNulo(99))
            .ReturnsAsync((Plano?)null);

        var resultado = await _sut.LimiteAtingidoAsync(1, "profissionais");

        Assert.That(resultado, Is.True);
    }

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimiteProfissionaisNulo_RetornaFalse()
    {
        // Plano ilimitado (LimiteProfissionais = null) deve retornar false sem consultar o banco.
        var assinatura = Assinatura.IniciarTrial(1, 10, TimeSpan.FromDays(14));
        var planoIlimitado = Plano.Criar("Enterprise", 999m, null, null, null); // LimiteProfissionais = null

        _assinaturaRepo
            .Setup(r => r.ObterPorEstabelecimentoOuNulo(1))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdOuNulo(10))
            .ReturnsAsync(planoIlimitado);

        var resultado = await _sut.LimiteAtingidoAsync(1, "profissionais");

        Assert.That(resultado, Is.False);
    }

    [Test]
    public async Task LimiteAtingidoAsync_PlanoComLimitePacientesNulo_RetornaFalse()
    {
        var assinatura = Assinatura.IniciarTrial(1, 10, TimeSpan.FromDays(14));
        var planoIlimitado = Plano.Criar("Enterprise", 999m, null, null, null);

        _assinaturaRepo
            .Setup(r => r.ObterPorEstabelecimentoOuNulo(1))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdOuNulo(10))
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
        var assinatura = Assinatura.IniciarTrial(1, 10, TimeSpan.FromDays(14));
        var plano = Plano.Criar("Basico", 49m, 2, 50, null);

        _assinaturaRepo
            .Setup(r => r.ObterPorEstabelecimentoOuNulo(1))
            .ReturnsAsync(assinatura);
        _planoRepo
            .Setup(r => r.ObterPorIdOuNulo(10))
            .ReturnsAsync(plano);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.LimiteAtingidoAsync(1, "desconhecido"));

        Assert.That(ex.Message, Does.Contain("Recurso desconhecido"));
    }
}
