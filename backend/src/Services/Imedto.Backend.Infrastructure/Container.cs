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
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>());
        });
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

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
            client.DefaultRequestHeaders.Add("apikey", opts.ServiceRoleKey);
        });

        services.AddScoped<IAuthService, SupabaseAuthService>();
    }
}

/// <summary>
/// Wrapper tipado para a connection string usada por query repositories (Dapper).
/// Evita coleção conflitante de <c>string</c> no DI e torna a intenção explícita.
/// </summary>
public sealed record AppReadConnectionString(string Value);
