using System.Net;
using System.Text;
using System.Text.Json;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Infrastructure.Migracao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Imedto.Backend.Test.Infrastructure.Migracao;

/// <summary>
/// Testes de resiliência do AnthropicMapeadorDeMigracao — addendum 5 (CA86-CA89).
/// Validam retry/backoff, Retry-After e permanência de 4xx≠429.
/// </summary>
[TestFixture]
public class AnthropicMapeadorRetryTests
{
    // Resposta JSON mínima que a Anthropic retorna com mapeamento de bloco.
    private const string RespostaOk = """
        {
          "content": [
            {
              "type": "text",
              "text": "{\"entidade_classificada\":\"paciente\",\"confianca_classificacao\":0.9,\"de_para\":{\"nome\":\"nome\"},\"confianca\":0.9,\"duvidas\":[]}"
            }
          ]
        }
        """;

    private static EsquemaDeArquivo EsquemaSimples() => new()
    {
        Cabecalhos = ["nome", "cpf"],
        AmostraMascarada = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["nome"] = "REDACTED", ["cpf"] = "***" }
        }
    };

    private static IConfiguration CriarConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ia:AnthropicApiKey"] = "test-key",
                ["Ia:Modelo"] = "claude-test",
            })
            .Build();
    }

    private static AnthropicMapeadorDeMigracao CriarSut(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };
        factory.Setup(f => f.CreateClient("Anthropic")).Returns(client);
        return new AnthropicMapeadorDeMigracao(
            factory.Object,
            CriarConfig(),
            NullLogger<AnthropicMapeadorDeMigracao>.Instance);
    }

    // ─── CA86 — retry em 429 com sucesso na 2ª tentativa ────────────────────

    [Test]
    public async Task InferirBlocoAsync_429NaPrimeira_SucedeNaSegunda()
    {
        // 1ª chamada → 429; 2ª chamada → 200 com mapeamento.
        var chamadas = 0;
        var handler = new DelegatingHandlerFake(req =>
        {
            chamadas++;
            if (chamadas == 1)
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespostaOk, Encoding.UTF8, "application/json")
            };
        });

        var sut = CriarSut(handler);
        var resultado = await sut.InferirBlocoAsync(EsquemaSimples(), "hint-teste");

        Assert.That(chamadas, Is.EqualTo(2), "Deve ter feito exatamente 2 chamadas (retry na 2ª).");
        Assert.That(resultado.EntidadeClassificada, Is.EqualTo(EntidadesCanônicas.Paciente));
    }

    // ─── CA87 — respeita Retry-After: X segundos ─────────────────────────────

    [Test]
    public async Task InferirBlocoAsync_429ComRetryAfter_AguardaTempoIndicado()
    {
        var chamadas = 0;
        var handler = new DelegatingHandlerFake(req =>
        {
            chamadas++;
            if (chamadas == 1)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                // Retry-After: 0 segundos (para não travar o teste)
                resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.FromMilliseconds(10));
                return resp;
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(RespostaOk, Encoding.UTF8, "application/json")
            };
        });

        var sut = CriarSut(handler);
        var resultado = await sut.InferirBlocoAsync(EsquemaSimples(), "hint-retry-after");

        Assert.That(chamadas, Is.EqualTo(2), "Deve ter retentado após o Retry-After.");
        Assert.That(resultado.EntidadeClassificada, Is.EqualTo(EntidadesCanônicas.Paciente));
    }

    // ─── CA88 — 401 é permanente, não retenta ────────────────────────────────

    [Test]
    public void InferirBlocoAsync_401_NaoRetenta_FalhaImediata()
    {
        var chamadas = 0;
        var handler = new DelegatingHandlerFake(req =>
        {
            chamadas++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var sut = CriarSut(handler);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.InferirBlocoAsync(EsquemaSimples(), "hint-401"),
            "Deve lançar imediatamente sem retry em 401.");

        // O Assert.ThrowsAsync retorna a tarefa — verificamos após.
        Assert.That(chamadas, Is.EqualTo(1), "401 é permanente — exatamente 1 chamada, sem retry.");
    }

    [Test]
    public void InferirBlocoAsync_403_NaoRetenta_FalhaImediata()
    {
        var chamadas = 0;
        var handler = new DelegatingHandlerFake(req =>
        {
            chamadas++;
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        });

        var sut = CriarSut(handler);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.InferirBlocoAsync(EsquemaSimples(), "hint-403"));

        Assert.That(chamadas, Is.EqualTo(1), "403 é permanente — exatamente 1 chamada, sem retry.");
    }

    // ─── CA89 — teto de 5 tentativas ─────────────────────────────────────────

    [Test]
    public void InferirBlocoAsync_429Persistente_Falha_AposTodasAsTentativas()
    {
        var chamadas = 0;
        var handler = new DelegatingHandlerFake(req =>
        {
            chamadas++;
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        });

        var sut = CriarSut(handler);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.InferirBlocoAsync(EsquemaSimples(), "hint-429-persistente"),
            "Deve lançar após esgotar tentativas.");

        Assert.That(chamadas, Is.EqualTo(5), "Exatamente 5 tentativas (teto CA89).");
    }

    [Test]
    public void InferirBlocoAsync_RedeFailTodas_Falha_AposTodasAsTentativas()
    {
        var chamadas = 0;
        var handler = new DelegatingHandlerFake(req =>
        {
            chamadas++;
            throw new HttpRequestException("Rede indisponível");
        });

        var sut = CriarSut(handler);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.InferirBlocoAsync(EsquemaSimples(), "hint-rede"));

        Assert.That(chamadas, Is.EqualTo(5), "Exatamente 5 tentativas em falha de rede.");
    }

    // ─── Handler fake para testes ─────────────────────────────────────────────

    private sealed class DelegatingHandlerFake : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public DelegatingHandlerFake(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_handler(request));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }
}
