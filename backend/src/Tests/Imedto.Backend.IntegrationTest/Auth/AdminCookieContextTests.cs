using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace Imedto.Backend.IntegrationTest.Auth;

/// <summary>
/// Regressão para o bug onde GET /api/admin/* com SOMENTE o cookie access-token
/// de usuário comum retornava 403 em vez de 401.
///
/// Causa-raiz: o OnMessageReceived no Program.cs caía no fallback do cookie
/// access-token quando admin-access-token não existia. O token regular
/// autenticava (RequireAuthenticatedUser passava) mas falhava na claim
/// imedto_admin → 403 Forbidden — irrecuperável pelo interceptor do front.
///
/// Comportamento correto pós-fix: sem admin-access-token → request anônimo
/// → 401 Unauthorized → interceptor dispara refresh com admin-refresh-token.
///
/// Estratégia: TestServer minimalista que replica as partes relevantes do
/// Program.cs (OnMessageReceived + políticas ImedtoAdmin / ImedtoAdminChangePassword)
/// sem precisar subir Postgres, S3, etc. — executável no CI sem dependências.
/// </summary>
[TestFixture]
public class AdminCookieContextTests
{
    // Chave ECDSA P-256 de teste — par gerado exclusivamente para os testes,
    // nunca usado em produção. A mesma chave privada usada em ImedtoAdminTokenIssuerTests.
    private const string TestPrivateKeyPem = """
        -----BEGIN PRIVATE KEY-----
        MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQg4rVhbCXKM+68CDIH
        LnPByf2Mh9DSwshlql9sTciawGKhRANCAARfibrvziUu4MYmCjD+a8slWe2e6bQO
        NdAXlzYAoIP93/B+BmfjGg0K5FeJd/VdFA8SeYfHlPPa2l4EPtDnvcs4
        -----END PRIVATE KEY-----
        """;

    private const string TestPublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEX4m6784lLuDGJgow/mvLJVntnum0
        DjXQF5c2AKCD/d/wfgZn4xoNCuRXiXf1XRQPEnmHx5Tz2tpeBD7Q573LOA==
        -----END PUBLIC KEY-----
        """;

    private const string AdminClaim = "imedto_admin";
    private const string Issuer = "imedto-backend";
    private const string Audience = "imedto-app";

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task ConfigurarServidor()
    {
        _app = CriarApp();
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

    // -------------------------------------------------------------------------
    // Teste principal: regressão do bug 403 → deve ser 401
    // -------------------------------------------------------------------------

    [Test]
    public async Task AdminEndpoint_ComApenasAccessTokenDeUsuarioComum_Retorna401()
    {
        // Antes do fix, esse cenário retornava 403:
        // o token regular autenticava mas falhava na claim imedto_admin.
        // Após o fix, o OnMessageReceived ignora access-token em rotas /api/admin/*
        // → request anônimo → 401.
        var tokenUsuarioComum = EmitirTokenUsuarioComum(Guid.NewGuid(), "usuario@exemplo.com");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/teste");
        request.Headers.Add("Cookie", $"access-token={tokenUsuarioComum}");

        var resposta = await _client.SendAsync(request);

        Assert.That((int)resposta.StatusCode, Is.EqualTo(401),
            "GET /api/admin/* com apenas cookie access-token de usuário comum " +
            "deve retornar 401 (não 403). O 403 era o bug: o interceptor do front " +
            "só recupera 401 via admin-refresh-token.");
    }

    // -------------------------------------------------------------------------
    // Caminho feliz: admin-access-token válido → 200
    // -------------------------------------------------------------------------

    [Test]
    public async Task AdminEndpoint_ComAdminAccessTokenValido_Retorna200()
    {
        var tokenAdmin = EmitirTokenAdmin(Guid.NewGuid(), "admin@imedto.com");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/teste");
        request.Headers.Add("Cookie", $"admin-access-token={tokenAdmin}");

        var resposta = await _client.SendAsync(request);

        Assert.That((int)resposta.StatusCode, Is.EqualTo(200),
            "GET /api/admin/* com admin-access-token válido deve retornar 200.");
    }

    // -------------------------------------------------------------------------
    // Nenhum cookie → 401 (não quebrou)
    // -------------------------------------------------------------------------

    [Test]
    public async Task AdminEndpoint_SemNenhumCookie_Retorna401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/teste");

        var resposta = await _client.SendAsync(request);

        Assert.That((int)resposta.StatusCode, Is.EqualTo(401),
            "GET /api/admin/* sem nenhum cookie deve retornar 401.");
    }

    // -------------------------------------------------------------------------
    // Rota normal com access-token regular continua 200 (não quebrou o caminho comum)
    // -------------------------------------------------------------------------

    [Test]
    public async Task EndpointNormal_ComAccessTokenDeUsuarioComum_Retorna200()
    {
        var tokenUsuarioComum = EmitirTokenUsuarioComum(Guid.NewGuid(), "usuario@exemplo.com");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuario/perfil");
        request.Headers.Add("Cookie", $"access-token={tokenUsuarioComum}");

        var resposta = await _client.SendAsync(request);

        Assert.That((int)resposta.StatusCode, Is.EqualTo(200),
            "GET /api/usuario/* com access-token de usuário comum deve continuar 200. " +
            "O fix não deve afetar rotas não-admin.");
    }

    // -------------------------------------------------------------------------
    // Admin com ambos os cookies: admin-access-token tem prioridade → 200
    // -------------------------------------------------------------------------

    [Test]
    public async Task AdminEndpoint_ComAmbosCookies_AdminTemPrioridade_Retorna200()
    {
        var tokenAdmin = EmitirTokenAdmin(Guid.NewGuid(), "admin@imedto.com");
        var tokenUsuario = EmitirTokenUsuarioComum(Guid.NewGuid(), "usuario@exemplo.com");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/teste");
        // Ambos os cookies presentes: admin tem prioridade
        request.Headers.Add("Cookie", $"admin-access-token={tokenAdmin}; access-token={tokenUsuario}");

        var resposta = await _client.SendAsync(request);

        Assert.That((int)resposta.StatusCode, Is.EqualTo(200),
            "GET /api/admin/* com ambos os cookies deve usar o admin-access-token " +
            "e retornar 200.");
    }

    // =========================================================================
    // Infraestrutura do TestServer
    // =========================================================================

    /// <summary>
    /// Monta um WebApplication minimalista que replica exatamente as partes
    /// relevantes do Program.cs: OnMessageReceived (com o fix) + políticas
    /// ImedtoAdmin e a política de usuário comum. Sem Postgres, S3 ou EF.
    /// </summary>
    private WebApplication CriarApp()
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(TestPublicKeyPem.AsSpan());
        var signingKey = new ECDsaSecurityKey(ecdsa);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // Replica EXATAMENTE o OnMessageReceived com o fix aplicado.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var path = ctx.HttpContext.Request.Path;

                        if (path.StartsWithSegments("/api/admin"))
                        {
                            var adminCookie = ctx.Request.Cookies["admin-access-token"];
                            if (!string.IsNullOrEmpty(adminCookie))
                            {
                                ctx.Token = adminCookie;
                                return Task.CompletedTask;
                            }

                            var adminHeader = ctx.Request.Headers.Authorization.ToString();
                            if (adminHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                ctx.Token = adminHeader["Bearer ".Length..].Trim();

                            // Deliberadamente NÃO lemos o cookie access-token aqui.
                            return Task.CompletedTask;
                        }

                        var cookie = ctx.Request.Cookies["access-token"];
                        if (!string.IsNullOrEmpty(cookie))
                        {
                            ctx.Token = cookie;
                            return Task.CompletedTask;
                        }

                        var header = ctx.Request.Headers.Authorization.ToString();
                        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Token = header["Bearer ".Length..].Trim();
                            return Task.CompletedTask;
                        }

                        if (path.StartsWithSegments("/hubs"))
                        {
                            var qs = ctx.Request.Query["access_token"].ToString();
                            if (!string.IsNullOrEmpty(qs))
                                ctx.Token = qs;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(opts =>
        {
            opts.AddPolicy("ImedtoAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim(AdminClaim, "true"));

            opts.AddPolicy("UsuarioComum", policy =>
                policy.RequireAuthenticatedUser());
        });

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        // Endpoint de admin protegido pela policy ImedtoAdmin
        app.MapGet("/api/admin/teste",
            [Authorize(Policy = "ImedtoAdmin")] () => Results.Ok(new { ok = true }));

        // Endpoint de usuário comum protegido por autenticação simples
        app.MapGet("/api/usuario/perfil",
            [Authorize(Policy = "UsuarioComum")] () => Results.Ok(new { ok = true }));

        return app;
    }

    /// <summary>Emite um JWT de usuário comum — sem claim imedto_admin.</summary>
    private string EmitirTokenUsuarioComum(Guid usuarioId, string email)
    {
        var claims = new[]
        {
            new Claim("sub", usuarioId.ToString()),
            new Claim("email", email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        return EmitirToken(claims);
    }

    /// <summary>Emite um JWT de administrador — com claim imedto_admin = "true".</summary>
    private string EmitirTokenAdmin(Guid adminId, string email)
    {
        var claims = new[]
        {
            new Claim("sub", adminId.ToString()),
            new Claim("email", email),
            new Claim(AdminClaim, "true"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        return EmitirToken(claims);
    }

    private string EmitirToken(IEnumerable<Claim> claims)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(TestPrivateKeyPem.AsSpan());
        var signingKey = new ECDsaSecurityKey(ecdsa);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
