using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.Infrastructure.Tenancy;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Infrastructure;

/// <summary>
/// Registra serviços de infraestrutura: DbContext (EF Core / Npgsql), UnitOfWork, auth service.
/// Repositórios e query repositórios específicos de cada domínio são adicionados aqui conforme são criados.
/// O Composition Root completo (buses + handlers) fica em API/Container.cs.
///
/// MIGRATIONS (Postgres via Supabase) — fluxo DUPLO:
/// 1. dotnet ef migrations add &lt;Nome&gt; --project Services/Imedto.Backend.Infrastructure --startup-project Services/Imedto.Backend.API --output-dir Database/Migrations
/// 2. dotnet ef migrations script &lt;Prev&gt; &lt;Next&gt; --idempotent --output /tmp/next.sql → copiar para supabase/migrations/&lt;TS&gt;_descricao.sql (remover BEGIN/COMMIT)
/// 3. supabase db push — aplica no projeto remoto
/// </summary>
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

        // Type handlers globais do Dapper — registra uma vez no startup.
        DapperTypeHandlers.Registrar();

        // Interceptor que bloqueia hard delete em ISoftDeletable e registra audit.
        // Singleton: o interceptor não tem state per-request — resolve ICurrentTenantAccessor
        // em runtime via IHttpContextAccessor (scoped do RequestServices). Trocar para
        // Scoped aqui voltaria a criar captive dependency sob AddDbContextPool.
        // Factory explícita: SoftDeleteInterceptor tem dois construtores públicos
        // (produção com IHttpContextAccessor, teste com ICurrentTenantAccessor) e o
        // CallSiteFactory do DI built-in não desambigua sozinho — registra como factory.
        services.AddSingleton(sp => new SoftDeleteInterceptor(
            sp.GetRequiredService<AppReadConnectionString>(),
            sp.GetRequiredService<IHttpContextAccessor>()));

        // Pool de DbContext: reduz alocação por-request. Requer:
        // - AppDbContext sem state de instância além de DbContextOptions (confirmado: só DbSets).
        // - Nenhum serviço scoped capturado no construtor do interceptor (resolvido em runtime).
        services.AddDbContextPool<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            // sp aqui é o root provider — só pode resolver singletons. SoftDeleteInterceptor é singleton.
            options.AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>());
        }, poolSize: 128);
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddScoped<Imedto.Backend.SharedKernel.Domain.IDomainEventDispatcher, Database.EfDomainEventDispatcher>();

        // Connection string disponível para query repositories (Dapper).
        services.AddSingleton(new AppReadConnectionString(connectionString));

        // Usuarios
        services.AddScoped<Domain.Usuarios.IUsuarioRepository, Database.Repositories.UsuarioRepository>();

        // Estabelecimentos
        services.AddScoped<Domain.Estabelecimentos.IEstabelecimentoRepository, Database.Repositories.EstabelecimentoRepository>();
        services.AddScoped<Domain.Unidades.IUnidadeRepository, Database.Repositories.UnidadeRepository>();
        services.AddScoped<Domain.Salas.ISalaRepository, Database.Repositories.SalaRepository>();

        // Profissionais
        services.AddScoped<Domain.Profissionais.IProfissionalRepository, Database.Repositories.ProfissionalRepository>();

        // Modelos de permissão + Vínculos
        services.AddScoped<Domain.ModelosPermissao.IModeloPermissaoRepository, Database.Repositories.ModeloPermissaoRepository>();
        services.AddScoped<Domain.Vinculos.IVinculoRepository, Database.Repositories.VinculoRepository>();
        services.AddScoped<Domain.Vinculos.ISolicitacaoVinculoRepository, Database.Repositories.SolicitacaoVinculoRepository>();

        // Pacientes
        services.AddScoped<Domain.Pacientes.IPacienteRepository, Database.Repositories.PacienteRepository>();
        services.AddScoped<Domain.Pacientes.IPacienteAcessoLogService, Database.Repositories.PacienteAcessoLogService>();

        // Prontuários (templates + pool + prontuário + evoluções)
        services.AddScoped<Domain.Prontuarios.IModeloDeProntuarioRepository, Database.Repositories.ModeloDeProntuarioRepository>();
        services.AddScoped<Domain.Prontuarios.IProntuarioVariavelPoolRepository, Database.Repositories.ProntuarioVariavelPoolRepository>();
        services.AddScoped<Domain.Prontuarios.IProntuarioRepository, Database.Repositories.ProntuarioRepository>();
        services.AddScoped<Domain.Prontuarios.IProntuarioEvolucaoRepository, Database.Repositories.ProntuarioEvolucaoRepository>();
        services.AddScoped<Domain.Prontuarios.IProntuarioAcessoLogService, Database.Repositories.ProntuarioAcessoLogService>();
        services.AddScoped<Domain.Prontuarios.IProntuarioAnexoRepository, Database.Repositories.ProntuarioAnexoRepository>();
        services.AddScoped<Domain.Prontuarios.IAnexoStorageService, Infrastructure.Storage.SupabaseStorageService>();

        // Exame físico (item 3.2) — escrita via EF, leitura via Dapper.
        services.AddScoped<Domain.Prontuarios.IExameFisicoRepository, Database.Repositories.ExameFisicoRepository>();
        services.AddScoped<Database.Repositories.ExameFisicoQueryRepository>();

        // Storage de fotos públicas (avatar de profissional, logo de estabelecimento)
        services.AddScoped<Domain.Common.IFotoStorageService, Infrastructure.Storage.SupabaseFotoStorageService>();

        // Agendamentos
        services.AddScoped<Domain.Agendamentos.IAgendamentoRepository, Database.Repositories.AgendamentoRepository>();

        // Inventário
        services.AddScoped<Domain.Inventario.IItemInventarioRepository, Database.Repositories.ItemInventarioRepository>();
        services.AddScoped<Domain.Inventario.IMovimentacaoEstoqueRepository, Database.Repositories.MovimentacaoEstoqueRepository>();

        // Orçamentos
        services.AddScoped<Domain.Orcamentos.IOrcamentoRepository, Database.Repositories.OrcamentoRepository>();

        // Cirurgias (item 3.3.A)
        services.AddScoped<Domain.Cirurgias.IProcedimentoCirurgicoRepository, Database.Repositories.ProcedimentoCirurgicoRepository>();

        // Financeiro
        services.AddScoped<Domain.Financeiro.ILancamentoRepository, Database.Repositories.LancamentoRepository>();

        // Dashboard & Relatórios (query-only, singleton)
        // — repositórios registrados no Container da API junto com os handlers

        // Auditoria (registro independente — usado pelo SoftDeleteInterceptor)
        services.AddScoped<Domain.Auditoria.IAuditDeleteAttemptRepository, Database.Repositories.AuditDeleteAttemptRepository>();

        // Tenancy (multi-tenant por estabelecimento)
        services.AddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();
        services.AddScoped<ITenantAccessResolver, TenantAccessResolver>();

        RegistrarAuth(services, configuration);

        return services;
    }

    private static void RegistrarAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.Section));
        services.Configure<Storage.StorageOptions>(configuration.GetSection(Storage.StorageOptions.Section));

        services.AddHttpClient("supabase", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
            client.BaseAddress = new Uri(opts.Url);
            // Login/refresh são <2s normalmente; default de 100s deixa request travado em
            // glitch de rede ocupando thread/socket por muito tempo. 10s fecha rápido.
            client.Timeout = TimeSpan.FromSeconds(10);
            // Default 'apikey' eh o anon_key (publica) — endpoints publicos (signup,
            // login, refresh, recover) usam so isso. Endpoints admin (delete user,
            // generate_link, admin/users) passam ServiceRoleKey explicitamente em
            // header Authorization Bearer no proprio request — nao colocar service
            // role como default bypassa rate limits/policies do Supabase.
            // Fallback para string vazia evita NullReference no startup quando o
            // appsettings local nao tem AnonKey configurada (ambiente parcial); em
            // runtime real essa chamada vai falhar com 401 do Supabase, que eh
            // muito mais seguro do que rodar com service_role default.
            client.DefaultRequestHeaders.Add("apikey", opts.AnonKey ?? string.Empty);
        })
        // Resilience: retry leve para 5xx/timeout/network + circuit breaker.
        // Polly v8 nativo do .NET 10 — protege fluxo de auth de glitches transientes
        // do Supabase Auth sem empilhar threads.
        .AddStandardResilienceHandler();

        services.AddScoped<IAuthService, SupabaseAuthService>();
    }
}

/// <summary>
/// Wrapper tipado para a connection string usada por query repositories (Dapper).
/// Evita coleção conflitante de <c>string</c> no DI e torna a intenção explícita.
/// </summary>
public sealed record AppReadConnectionString(string Value);
