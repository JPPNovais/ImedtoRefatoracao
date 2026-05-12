using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Imedto.Backend.API;
using Imedto.Backend.API.Filters;
using Imedto.Backend.API.Hubs;
using Imedto.Backend.API.Jobs;
using Imedto.Backend.API.Logging;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Filters;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Cultura pt-BR global
var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// --- Logging estruturado (Serilog) ---
// Dev: console legível (formatação default).
// Prod: console com JSON compacto (CompactJsonFormatter) — ideal para coletores (Loki/CloudWatch/etc).
// Enrichers: ambiente, machine name, e RemovePIIEnricher (LGPD — mascara cpf/email/telefone/etc).
// O template padrão do UseSerilogRequestLogging não loga body/query/headers — seguro por padrão.
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithEnvironmentName()
       .Enrich.WithMachineName()
       .Enrich.With<RemovePIIEnricher>()
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
       .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);

    if (ctx.HostingEnvironment.IsDevelopment())
    {
        cfg.WriteTo.Console();
    }
    else
    {
        cfg.WriteTo.Console(new CompactJsonFormatter());
    }
});

// --- Autenticação JWT (BFF pattern, auth local) ---
// JWT é emitido e validado localmente (ECDSA P-256). Chaves vêm das configs
// Auth:Jwt:PrivateKeyPem e Auth:Jwt:PublicKeyPem (populadas a partir do AWS
// SSM Parameter Store).
// Produção: token lido do cookie HttpOnly (frontend nunca vê o token).
// Desenvolvimento/Swagger: aceita também o header Authorization: Bearer <token>
//   → copie o accessToken da resposta de POST /api/auth/login e cole no Authorize do Swagger.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Auth:Jwt:Issuer"]
            ?? throw new InvalidOperationException("Auth:Jwt:Issuer não configurado.");
        var audience = builder.Configuration["Auth:Jwt:Audience"]
            ?? throw new InvalidOperationException("Auth:Jwt:Audience não configurado.");
        var publicKeyPem = builder.Configuration["Auth:Jwt:PublicKeyPem"]
            ?? throw new InvalidOperationException("Auth:Jwt:PublicKeyPem não configurado.");

        // pull-secrets.sh codifica PEMs como uma única linha com '\n' literal
        // pra caber no .env do docker-compose. Reverter pra newlines reais
        // antes do ImportFromPem.
        publicKeyPem = publicKeyPem.Replace("\\n", "\n");

        var ecdsa = System.Security.Cryptography.ECDsa.Create();
        ecdsa.ImportFromPem(publicKeyPem.AsSpan());
        var signingKey = new ECDsaSecurityKey(ecdsa);

        options.RequireHttpsMetadata = false; // chave local, sem JWKS remoto

        // JWT emitido com claim "sub" — evita mapear pra ClaimTypes.NameIdentifier.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidAlgorithms = new[] { SecurityAlgorithms.EcdsaSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Cookie HttpOnly tem prioridade (frontend / produção)
                var cookie = ctx.Request.Cookies["access-token"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    ctx.Token = cookie;
                    return Task.CompletedTask;
                }

                // Fallback 1: Authorization header (Swagger / testes / integrações)
                var header = ctx.Request.Headers.Authorization.ToString();
                if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Token = header["Bearer ".Length..].Trim();
                    return Task.CompletedTask;
                }

                // Fallback 2: query string em conexões SignalR/WebSocket — padrão oficial
                // do framework, já que alguns clientes WS não propagam cookie/header de auth no
                // handshake. Restrito a /hubs/* para evitar expor tokens em URL de endpoints REST.
                var path = ctx.HttpContext.Request.Path;
                if (path.StartsWithSegments("/hubs"))
                {
                    var qs = ctx.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(qs))
                        ctx.Token = qs;
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuth");
                // LGPD: nunca logar Authorization completo. JWT contem PII
                // (email/sub) no payload. Logamos so prefixo do token (suficiente p/
                // correlacao em log) e o erro do middleware.
                var authHeader = ctx.Request.Headers.Authorization.ToString();
                var tokenPreview = authHeader.Length > 20
                    ? authHeader[..20] + "…"
                    : "(vazio)";
                logger.LogWarning(ctx.Exception,
                    "JWT falhou: {Error}. TokenPreview: {TokenPreview}",
                    ctx.Exception?.Message,
                    tokenPreview);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

// --- Controllers com filtros globais ---
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.Filters.Add<UnitOfWorkFilter>();
    options.Filters.Add<IdempotencyFilter>();
    options.Filters.Add<OnboardingCompletadoFilter>();
}).AddJsonOptions(opts =>
{
    // Normaliza DateTime/DateTime? para UTC ao deserializar — colunas Postgres
    // `timestamp with time zone` exigem Kind=Utc; sem isso, datas ISO sem TZ
    // (ex: "2026-05-26") chegavam com Kind=Unspecified e o Npgsql lançava 500.
    opts.JsonSerializerOptions.Converters.Add(new Imedto.Backend.API.Json.UtcDateTimeJsonConverter());
    opts.JsonSerializerOptions.Converters.Add(new Imedto.Backend.API.Json.UtcNullableDateTimeJsonConverter());
});

// Padroniza a resposta de 400 (validação de modelo / parser JSON) no mesmo formato
// { tipo, mensagem } usado em BusinessException/ForbiddenException. Sem isso, requests
// com body inválido devolvem o ValidationProblemDetails default — que expõe detalhes
// internos do parser (Path: $.campo, LineNumber, BytePositionInLine).
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ctx =>
    {
        var primeiro = ctx.ModelState
            .FirstOrDefault(e => e.Value?.Errors.Count > 0)
            .Value?.Errors.FirstOrDefault();
        var mensagem = primeiro?.ErrorMessage;
        if (string.IsNullOrWhiteSpace(mensagem) || mensagem.StartsWith("The JSON value", StringComparison.Ordinal))
            mensagem = "Dados inválidos no corpo da requisição.";
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            tipo = "DadosInvalidos",
            mensagem
        });
    };
});

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Imedto CQRS API",
        Version = "v1",
        Description = """
            API com padrão DDD + CQRS e autenticação via BFF (Backend for Frontend).

            **Como autenticar no Swagger:**
            1. Chame `POST /api/auth/login` com e-mail e senha
            2. Copie o `access_token` retornado no campo `accessToken` da resposta de debug
               _(em produção o token fica apenas no cookie — aqui retornamos para facilitar testes)_
            3. Clique em **Authorize** (cadeado) e cole o token
            """
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token JWT local (ECDSA P-256). Obtenha em POST /api/auth/login."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// --- CORS com credenciais ---
// `Cors:AllowedOrigins`        → lista fixa de origens (ex: dev local).
// `Cors:AllowedOriginPatterns` → regex (úteis para previews dinâmicos da
//                                Vercel/Render: ^https://app-.*\.vercel\.app$).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

var allowedOriginPatterns = (builder.Configuration
    .GetSection("Cors:AllowedOriginPatterns")
    .Get<string[]>() ?? Array.Empty<string>())
    .Select(p => new System.Text.RegularExpressions.Regex(
        p,
        System.Text.RegularExpressions.RegexOptions.Compiled
        | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy
            .SetIsOriginAllowed(origin =>
                allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
                || allowedOriginPatterns.Any(rx => rx.IsMatch(origin)))
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition"));
});

// --- Rate limiting (proteção a /auth contra brute force + envenenamento de cache) ---
// Policies por janela deslizante de 60s, particionadas por IP do cliente.
// Em ambientes atrás de proxy reverso, X-Forwarded-For tem prioridade sobre RemoteIpAddress.
// Resposta 429 é genérica (sem PII) e o log estrutura apenas hash truncado do IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string ResolverIp(HttpContext ctx)
    {
        var fwd = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            // X-Forwarded-For pode vir com vários IPs separados por vírgula — usar o primeiro.
            var primeiro = fwd.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(primeiro)) return primeiro;
        }
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    static RateLimitPartition<string> CriarParticao(HttpContext ctx, int permitido)
        => RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ResolverIp(ctx),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitido,
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });

    options.AddPolicy("auth-login", ctx => CriarParticao(ctx, 5));
    options.AddPolicy("auth-refresh", ctx => CriarParticao(ctx, 10));
    options.AddPolicy("auth-sensitive", ctx => CriarParticao(ctx, 3));

    options.OnRejected = async (context, ct) =>
    {
        // Janela de 60s — o caller pode tentar novamente após esse intervalo na pior das hipóteses.
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        // Log: nunca o IP cru — apenas hash SHA256 truncado para correlacionar sem expor PII.
        var ipBruto = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                      ?? context.HttpContext.Connection.RemoteIpAddress?.ToString()
                      ?? "unknown";
        var hashIp = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ipBruto)))[..16];

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimit");
        logger.LogWarning(
            "Rate limit excedido. Endpoint={Endpoint} HashIp={HashIp}",
            context.HttpContext.Request.Path,
            hashIp);

        var payload = JsonSerializer.Serialize(new
        {
            mensagem = "Muitas tentativas. Tente novamente em alguns instantes."
        });
        await context.HttpContext.Response.WriteAsync(payload, ct);
    };
});

// --- SignalR (item 2.4 — realtime + item 4.6 — backplane Redis multi-instância) ---
// Hub único `EstabelecimentoHub` em /hubs/estabelecimento.
// Quando `Redis:ConnectionString` está preenchido, ativamos o backplane via Redis pub/sub
// (sincroniza grupos/conexões entre múltiplas instâncias). Caso contrário, o servidor cai
// no modo single-instance (correto e suficiente para dev/local; em prod é log de info).
// TODO infra: provisionar Redis em prod (Terraform/Bicep — fora do escopo desta task).
var redisConnection = builder.Configuration.GetSection("Redis:ConnectionString").Value;
var signalR = builder.Services.AddSignalR(options =>
{
    // Reduz reconexões/negotiate em rede instável e atrás de proxy. Cliente (realtimeService.ts)
    // tem que usar valores compatíveis: serverTimeoutInMilliseconds >= 2x KeepAliveInterval.
    options.KeepAliveInterval = TimeSpan.FromMinutes(5);
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
});
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalR.AddStackExchangeRedis(redisConnection, options =>
    {
        // Prefixo dedicado para evitar colisão com outras apps que compartilhem o mesmo Redis.
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("imedto-signalr");
    });
}
else if (!builder.Environment.IsDevelopment())
{
    // Apenas em prod log informativo — em dev é o comportamento esperado.
    var bootLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("SignalR");
    bootLogger.LogInformation(
        "SignalR rodando sem backplane Redis (Redis:ConnectionString vazio). "
        + "Multi-instância não funcionará — defina a connection string para habilitar.");
}

// --- Composition Root ---
builder.Services.Install(builder.Configuration);
builder.Services.AddHostedService<AutomacaoJob>();

// --- OpenTelemetry (traces + metrics) ---
// ServiceName configurável; Endpoint vazio => não registra exporter (útil em Dev/Test).
// Instrumentação automática para AspNetCore, EF Core, HttpClient e runtime (.NET GC/threads).
var otelServiceName = builder.Configuration["Otel:ServiceName"] ?? "imedto-backend";
var otelEndpoint = builder.Configuration["Otel:Endpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: otelServiceName))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation()
         .AddEntityFrameworkCoreInstrumentation()
         .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otelEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint));
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation()
         .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(otelEndpoint))
            m.AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint));
    });

// --- Health Checks ---
// /health (liveness) — 200 se app vivo, sem dependências externas.
// /health/ready (readiness) — checa Postgres (tag "ready").
var healthChecksBuilder = builder.Services.AddHealthChecks();
var healthDbConn = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(healthDbConn))
{
    healthChecksBuilder.AddNpgSql(
        connectionString: healthDbConn,
        name: "postgres",
        tags: new[] { "ready" });
}

// --- HTTP client para Anthropic IA ---
builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    client.DefaultRequestHeaders.Add("x-api-key",
        builder.Configuration["Ia:AnthropicApiKey"] ?? string.Empty);
    client.Timeout = TimeSpan.FromMinutes(3);
});

// --- HTTP client para Resend (email transacional) ---
builder.Services.AddHttpClient("Resend", client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});

var app = builder.Build();

// --- Middleware de exceções (deve ser o primeiro no pipeline) ---
// UseExceptionHandler envolve todo o pipeline num try-catch real do C#,
// impedindo que exceções cheguem ao CLR como "user-unhandled" e pausem o debugger.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        ctx.Response.ContentType = "application/json";

        if (ex is BusinessException businessEx)
        {
            ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await ctx.Response.WriteAsJsonAsync(new
            {
                tipo = "ErroDeNegocio",
                mensagem = businessEx.Message
            });
            return;
        }

        var log = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        log.LogError(ex, "Erro não tratado: {Mensagem}", ex?.Message);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new
        {
            tipo = "ErroInterno",
            mensagem = "Ocorreu um erro interno. Por favor, tente novamente."
        });
    });
});

// --- Swagger UI ---
app.UseSwagger();
app.UseSwaggerUI(ui =>
{
    ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Imedto CQRS v1");
    ui.RoutePrefix = "swagger";
    ui.DisplayRequestDuration();
    ui.DefaultModelsExpandDepth(-1);
});

app.MapGet("/", () => Results.Redirect("/swagger"));

// --- Security headers (LGPD + OWASP) ---
// Não aplicados em Development para não bloquear o Swagger UI (CSP bloquearia scripts/estilos).
if (!app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["X-XSS-Protection"] = "0";
        ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
        await next();
    });
}

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Popula ICurrentTenantAccessor.UsuarioId em toda request autenticada
// (defense-in-depth — handlers podem usar _tenant.UsuarioId em vez de
// confiar em campos do command). Posicionado APOS UseAuthentication
// porque depende de User.Claims populado.
app.UseMiddleware<Imedto.Backend.SharedKernel.Tenancy.CurrentUserMiddleware>();

// Log estruturado de requisicoes — template default nao inclui body/query/headers (seguro p/ LGPD).
// Posicionado APOS UseAuthentication para que User.Identity/claims fiquem disponiveis no log.
app.UseSerilogRequestLogging();

app.MapControllers();

// --- SignalR hub ---
// Path /hubs/estabelecimento. Auth via [Authorize] no hub + OnMessageReceived que aceita
// cookie HttpOnly (HTTP negotiate) ou ?access_token= (handshake WebSocket).
app.MapHub<EstabelecimentoHub>("/hubs/estabelecimento");

// --- Health endpoints ---
// Liveness: sempre 200 se o processo responde — não checa dependências.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

// Readiness: só checks com tag "ready" (Postgres). 503 se falhar.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();
