using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Imedto.Backend.IntegrationTest.Auth;

/// <summary>
/// Valida o comportamento de rate limiting nos endpoints de autenticação.
///
/// Estratégia: monta um TestServer minimalista que replica exatamente as políticas
/// definidas em Program.cs (auth-login: 5/60s, auth-refresh: 10/60s, auth-sensitive: 3/60s)
/// sem precisar subir toda a stack (Postgres, S3, etc.).
///
/// Esta abordagem é executável no CI sem dependências externas.
///
/// Para testar contra o servidor real (carga real via k6):
///   k6 run scripts/load-tests/auth-rate-limit.js
/// </summary>
[TestFixture]
public class RateLimitTests
{
    private HttpClient _client;
    private WebApplication _app;

    [OneTimeSetUp]
    public async Task ConfigurarServidor()
    {
        // Replica as políticas de rate limit de Program.cs em servidor isolado.
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Janela deslizante de 60s — mesmos parâmetros de produção.
            static RateLimitPartition<string> CriarParticao(HttpContext ctx, int permitido)
                => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: "ip-teste",   // chave fixa: todos os testes compartilham o mesmo "IP"
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitido,
                        Window = TimeSpan.FromSeconds(60),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    });

            options.AddPolicy("auth-login", ctx => CriarParticao(ctx, 5));
            options.AddPolicy("auth-refresh", ctx => CriarParticao(ctx, 10));
            options.AddPolicy("auth-sensitive", ctx => CriarParticao(ctx, 3));
        });

        _app = builder.Build();

        _app.UseRateLimiter();

        // Endpoints de stub que espelham os paths de produção.
        _app.MapPost("/api/auth/login", () => Results.UnprocessableEntity(new { mensagem = "Credenciais inválidas." }))
            .RequireRateLimiting("auth-login");

        _app.MapPost("/api/auth/refresh", () => Results.Unauthorized())
            .RequireRateLimiting("auth-refresh");

        _app.MapPost("/api/auth/signup", () => Results.UnprocessableEntity(new { mensagem = "E-mail já cadastrado." }))
            .RequireRateLimiting("auth-sensitive");

        _app.MapPost("/api/auth/forgot-password", () => Results.Ok())
            .RequireRateLimiting("auth-sensitive");

        await _app.StartAsync();

        _client = _app.GetTestClient();
    }

    [OneTimeTearDown]
    public async Task DesligarServidor()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.StopAsync();
    }

    // --------------------------------------------------------------------------
    // auth/login — limite 5/60s
    // --------------------------------------------------------------------------

    [Test]
    [Order(1)]
    public async Task Login_PrimeirasCincoTentativas_RetornamStatusNegocio()
    {
        // arrange
        var payload = JsonContent("{ \"email\": \"teste@imedto.com.br\", \"senha\": \"errada\" }");

        // act — 5 chamadas dentro do limite
        var respostas = new List<HttpStatusCode>();
        for (int i = 0; i < 5; i++)
        {
            var resposta = await _client.PostAsync("/api/auth/login", ClonarPayload(payload));
            respostas.Add((HttpStatusCode)resposta.StatusCode);
        }

        // assert — todas devem ser 4xx de negócio (não 429)
        Assert.That(respostas, Has.All.Matches<HttpStatusCode>(s => s != HttpStatusCode.TooManyRequests),
            "As primeiras 5 chamadas a /auth/login não devem ser bloqueadas por rate limit.");
    }

    [Test]
    [Order(2)]
    public async Task Login_SextaTentativa_Retorna429()
    {
        // arrange — esgota o limite (se o teste Order(1) rodou na mesma janela)
        // Para garantir isolamento, usa uma nova instância de servidor (chave diferente).
        var (clienteIsolado, app) = await CriarClienteIsolado("auth-login", 5);
        try
        {
            var payload = JsonContent("{ \"email\": \"teste@imedto.com.br\", \"senha\": \"errada\" }");

            // act — esgota o limite com 5 chamadas
            for (int i = 0; i < 5; i++)
                await clienteIsolado.PostAsync("/api/auth/login", ClonarPayload(payload));

            // 6ª chamada deve receber 429
            var resposta = await clienteIsolado.PostAsync("/api/auth/login", ClonarPayload(payload));

            // assert
            Assert.That((int)resposta.StatusCode, Is.EqualTo(429),
                "A 6ª chamada a /auth/login (acima do limite de 5) deve retornar 429.");
        }
        finally
        {
            clienteIsolado.Dispose();
            await app.StopAsync();
        }
    }

    // --------------------------------------------------------------------------
    // auth/refresh — limite 10/60s
    // --------------------------------------------------------------------------

    [Test]
    [Order(3)]
    public async Task Refresh_DezemPrimeirasChamas_RetornamStatusNegocio()
    {
        // arrange
        var (clienteIsolado, app) = await CriarClienteIsolado("auth-refresh", 10);
        try
        {
            // act — 10 chamadas dentro do limite
            var respostas = new List<HttpStatusCode>();
            for (int i = 0; i < 10; i++)
            {
                var resposta = await clienteIsolado.PostAsync("/api/auth/refresh", null);
                respostas.Add((HttpStatusCode)resposta.StatusCode);
            }

            // assert
            Assert.That(respostas, Has.All.Matches<HttpStatusCode>(s => s != HttpStatusCode.TooManyRequests),
                "As primeiras 10 chamadas a /auth/refresh não devem ser bloqueadas.");
        }
        finally
        {
            clienteIsolado.Dispose();
            await app.StopAsync();
        }
    }

    [Test]
    [Order(4)]
    public async Task Refresh_DecimaFirstaTentativa_Retorna429()
    {
        // arrange
        var (clienteIsolado, app) = await CriarClienteIsolado("auth-refresh", 10);
        try
        {
            for (int i = 0; i < 10; i++)
                await clienteIsolado.PostAsync("/api/auth/refresh", null);

            // act
            var resposta = await clienteIsolado.PostAsync("/api/auth/refresh", null);

            // assert
            Assert.That((int)resposta.StatusCode, Is.EqualTo(429),
                "A 11ª chamada a /auth/refresh (acima do limite de 10) deve retornar 429.");
        }
        finally
        {
            clienteIsolado.Dispose();
            await app.StopAsync();
        }
    }

    // --------------------------------------------------------------------------
    // auth-sensitive (signup / forgot-password) — limite 3/60s
    // --------------------------------------------------------------------------

    [Test]
    [Order(5)]
    public async Task Signup_TerceiraEUltimaTentativa_NaoRetorna429()
    {
        // arrange
        var (clienteIsolado, app) = await CriarClienteIsolado("auth-sensitive", 3);
        try
        {
            var payload = JsonContent("{ \"email\": \"novo@imedto.com.br\", \"senha\": \"Teste@123\" }");

            // act — 3 chamadas dentro do limite
            var respostas = new List<HttpStatusCode>();
            for (int i = 0; i < 3; i++)
            {
                var resposta = await clienteIsolado.PostAsync("/api/auth/signup", ClonarPayload(payload));
                respostas.Add((HttpStatusCode)resposta.StatusCode);
            }

            // assert
            Assert.That(respostas, Has.All.Matches<HttpStatusCode>(s => s != HttpStatusCode.TooManyRequests),
                "As primeiras 3 chamadas a /auth/signup não devem ser bloqueadas.");
        }
        finally
        {
            clienteIsolado.Dispose();
            await app.StopAsync();
        }
    }

    [Test]
    [Order(6)]
    public async Task Signup_QuartaTentativa_Retorna429()
    {
        // arrange
        var (clienteIsolado, app) = await CriarClienteIsolado("auth-sensitive", 3);
        try
        {
            var payload = JsonContent("{ \"email\": \"novo@imedto.com.br\", \"senha\": \"Teste@123\" }");

            // esgota o limite
            for (int i = 0; i < 3; i++)
                await clienteIsolado.PostAsync("/api/auth/signup", ClonarPayload(payload));

            // act — 4ª chamada
            var resposta = await clienteIsolado.PostAsync("/api/auth/signup", ClonarPayload(payload));

            // assert
            Assert.That((int)resposta.StatusCode, Is.EqualTo(429),
                "A 4ª chamada a /auth/signup (acima do limite de 3) deve retornar 429.");
        }
        finally
        {
            clienteIsolado.Dispose();
            await app.StopAsync();
        }
    }

    // --------------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------------

    /// <summary>
    /// Cria um servidor isolado com chave de partição única por teste,
    /// garantindo que o limite nunca seja compartilhado entre testes.
    /// </summary>
    private static async Task<(HttpClient, WebApplication)> CriarClienteIsolado(string politica, int limite)
    {
        var chavePartição = Guid.NewGuid().ToString();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            RateLimitPartition<string> Criar(HttpContext ctx, int permitido)
                => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: chavePartição,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitido,
                        Window = TimeSpan.FromSeconds(60),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    });

            options.AddPolicy("auth-login", ctx => Criar(ctx, politica == "auth-login" ? limite : 99));
            options.AddPolicy("auth-refresh", ctx => Criar(ctx, politica == "auth-refresh" ? limite : 99));
            options.AddPolicy("auth-sensitive", ctx => Criar(ctx, politica == "auth-sensitive" ? limite : 99));
        });

        var app = builder.Build();
        app.UseRateLimiter();

        app.MapPost("/api/auth/login", () => Results.UnprocessableEntity()).RequireRateLimiting("auth-login");
        app.MapPost("/api/auth/refresh", () => Results.Unauthorized()).RequireRateLimiting("auth-refresh");
        app.MapPost("/api/auth/signup", () => Results.UnprocessableEntity()).RequireRateLimiting("auth-sensitive");
        app.MapPost("/api/auth/forgot-password", () => Results.Ok()).RequireRateLimiting("auth-sensitive");

        await app.StartAsync();
        return (app.GetTestClient(), app);
    }

    private static StringContent JsonContent(string json)
        => new StringContent(json, Encoding.UTF8, "application/json");

    private static StringContent ClonarPayload(StringContent original)
    {
        // StringContent não pode ser reenviado — recria a cada chamada.
        var (conteudo, encoding, tipo) = (original.ReadAsStringAsync().Result, Encoding.UTF8, "application/json");
        return new StringContent(conteudo, encoding, tipo);
    }
}
