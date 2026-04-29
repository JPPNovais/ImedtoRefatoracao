using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Imedto.Backend.API;
using Imedto.Backend.API.Jobs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Filters;

var builder = WebApplication.CreateBuilder(args);

// Cultura pt-BR global
var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// --- Autenticação JWT (BFF pattern) ---
// O Supabase expõe discovery em {Authority}/.well-known/openid-configuration e assina com ES256 (JWKS).
// O middleware descobre as chaves sozinho a partir do Authority — não usamos IssuerSigningKey fixa.
// Produção: token lido do cookie HttpOnly (frontend nunca vê o token).
// Desenvolvimento/Swagger: aceita também o header Authorization: Bearer <token>
//   → copie o access_token da resposta de POST /api/auth/login e cole no Authorize do Swagger.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["Supabase:Authority"]
            ?? throw new InvalidOperationException("Supabase:Authority não configurado.");

        options.Authority = authority;
        options.RequireHttpsMetadata = true;

        // Supabase JWTs usam "sub" nativamente — evitamos o mapeamento default
        // para ClaimTypes.NameIdentifier (http://schemas.xmlsoap.org/...).
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = authority,
            ValidateAudience         = true,
            ValidAudience            = "authenticated",
            ValidateIssuerSigningKey = true,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
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

                // Fallback: Authorization header (Swagger / testes / integrações)
                var header = ctx.Request.Headers.Authorization.ToString();
                if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    ctx.Token = header["Bearer ".Length..].Trim();

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuth");
                logger.LogWarning(ctx.Exception,
                    "JWT falhou: {Error}. Token kid: {Kid}",
                    ctx.Exception?.Message,
                    ctx.Request.Headers.Authorization.ToString());
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// --- Controllers com filtros globais ---
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.Filters.Add<UnitOfWorkFilter>();
});

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Imedto CQRS API",
        Version     = "v1",
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
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Token JWT do Supabase. Obtenha em POST /api/auth/login."
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
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
        policy
            .WithOrigins(allowedOrigins)
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

// --- Composition Root ---
builder.Services.Install(builder.Configuration);
builder.Services.AddHostedService<AutomacaoJob>();

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
        ctx.Response.Headers["X-Content-Type-Options"]  = "nosniff";
        ctx.Response.Headers["X-Frame-Options"]         = "DENY";
        ctx.Response.Headers["Referrer-Policy"]         = "strict-origin-when-cross-origin";
        ctx.Response.Headers["X-XSS-Protection"]        = "0";
        ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
        await next();
    });
}

app.UseCors("CorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
