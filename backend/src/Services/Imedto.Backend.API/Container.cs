using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Imedto.Backend.Application.Auth.Queries;
using Imedto.Backend.Application.Automacoes.Commands;
using Imedto.Backend.Application.Automacoes.Queries;
using Imedto.Backend.Application.Agendamentos.Commands;
using Imedto.Backend.Application.Agendamentos.Events;
using Imedto.Backend.Application.Agendamentos.Queries;
using Imedto.Backend.Application.Inventario.Commands;
using Imedto.Backend.Application.Inventario.Events;
using Imedto.Backend.Application.Inventario.Queries;
using Imedto.Backend.Application.Inventario.Cadastros.Commands;
using Imedto.Backend.Application.Inventario.Cadastros.Queries;
using Imedto.Backend.Application.Dashboard;
using Imedto.Backend.Application.Financeiro.Commands;
using Imedto.Backend.Application.Financeiro.Events;
using Imedto.Backend.Application.Financeiro.Queries;
using Imedto.Backend.Application.Relatorios;
using Imedto.Backend.Application.Relatorios.Queries;
using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Application.Orcamentos.Events;
using Imedto.Backend.Application.Orcamentos.Queries;
using Imedto.Backend.Application.Orcamentos.Catalogos;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Infrastructure.Database.Repositories.OrcamentoCatalogos;
using Imedto.Backend.Application.ModelosPermissao.Commands;
using Imedto.Backend.Application.ModelosPermissao.Queries;
using Imedto.Backend.Application.Estabelecimentos.Commands;
using Imedto.Backend.Application.Estabelecimentos.Events;
using Imedto.Backend.Application.Estabelecimentos.Queries;
using Imedto.Backend.Application.Unidades.Commands;
using Imedto.Backend.Application.Unidades.Queries;
using Imedto.Backend.Application.Salas.Commands;
using Imedto.Backend.Application.Salas.Queries;
using Imedto.Backend.Application.Profissionais.Commands;
using Imedto.Backend.Application.Profissionais.Events;
using Imedto.Backend.Application.Profissionais.Queries;
using Imedto.Backend.Application.Onboarding.Commands;
using Imedto.Backend.Application.Usuarios.Commands;
using Imedto.Backend.Application.Usuarios.Events;
using Imedto.Backend.Application.Usuarios.Queries;
using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Application.Pacientes.Events;
using Imedto.Backend.Application.Pacientes.Queries;
using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Application.Prontuarios.Events;
using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Application.Receitas.Queries;
using Imedto.Backend.Application.Atestados.Commands;
using Imedto.Backend.Application.Atestados.Queries;
using Imedto.Backend.Application.PedidosExame.Commands;
using Imedto.Backend.Application.PedidosExame.Queries;
using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Application.Vinculos.Events;
using Imedto.Backend.Application.Notificacoes.Commands;
using Imedto.Backend.Application.Notificacoes.Queries;
using Imedto.Backend.Application.Automacoes.Events;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Notificacoes.Commands;
using Imedto.Backend.Contracts.Notificacoes.Queries;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Contracts.Atestados.Commands;
using Imedto.Backend.Contracts.Atestados.Queries;
using Imedto.Backend.Contracts.Atestados.Queries.Results;
using Imedto.Backend.Contracts.PedidosExame.Commands;
using Imedto.Backend.Contracts.PedidosExame.Queries;
using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.Contracts.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.Contracts.Unidades.Commands;
using Imedto.Backend.Contracts.Unidades.Queries;
using Imedto.Backend.Contracts.Unidades.Queries.Results;
using Imedto.Backend.Contracts.Salas.Commands;
using Imedto.Backend.Contracts.Salas.Queries;
using Imedto.Backend.Contracts.Salas.Queries.Results;
using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Contracts.Profissionais.Queries;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;
using Imedto.Backend.Contracts.Onboarding.Commands;
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Contracts.Usuarios.Queries;
using Imedto.Backend.Application.Vinculos.Queries;
using Imedto.Backend.Contracts.Auth.Queries;
using Imedto.Backend.Contracts.Auth.Queries.Results;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Pacientes.Events;
using Imedto.Backend.Domain.Profissionais.Events;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Contracts.Inventario.Commands;
using Imedto.Backend.Contracts.Inventario.Queries;
using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Contracts.Inventario.Cadastros.Queries;
using Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;
using Imedto.Backend.Contracts.Dashboard;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Contracts.Relatorios;
using Imedto.Backend.Contracts.Relatorios.Queries;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Contracts.Cirurgias.Queries;
using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Imedto.Backend.Application.Cirurgias.Commands;
using Imedto.Backend.Application.Cirurgias.Events;
using Imedto.Backend.Application.Cirurgias.Queries;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Cirurgias.Events;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Domain.Financeiro.Events;
using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.Application.Catalogo.Queries;
using Imedto.Backend.Domain.Catalogo;
using Imedto.Backend.Application.Lgpd.Commands;
using Imedto.Backend.Application.Lgpd.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.Contracts.Lgpd.Commands;
using Imedto.Backend.Contracts.Lgpd.Queries;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Queries;
using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Contracts.ModelosPermissao.Queries;
using Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Assinaturas;
using Imedto.Backend.Infrastructure.Assinaturas.Handlers;
using Imedto.Backend.Infrastructure.Bus;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Application.Assinaturas.Queries;
using Imedto.Backend.Contracts.Assinaturas.Queries;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Notificacoes.Events;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.Domain.Receitas.Events;
using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.Domain.Atestados.Events;
using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.Domain.PedidosExame.Events;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Termos.Events;
using Imedto.Backend.Application.Termos.Commands;
using Imedto.Backend.Application.Termos.Queries;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Infrastructure.Termos;
using Imedto.Backend.Application.AssinaturaDigital.Commands;
using Imedto.Backend.Application.AssinaturaDigital.Queries;
using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Contracts.AssinaturaDigital.Queries;
using Imedto.Backend.Application.Cobrancas.Commands;
using Imedto.Backend.Application.Cobrancas.Queries;
using Imedto.Backend.Application.Convenios.Commands;
using Imedto.Backend.Application.Convenios.Queries;
using Imedto.Backend.Application.PacienteConvenios.Commands;
using Imedto.Backend.Application.PacienteConvenios.Queries;
using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Contracts.Convenios.Queries;
using Imedto.Backend.Contracts.Convenios.Queries.Results;
using Imedto.Backend.Contracts.PacienteConvenios.Commands;
using Imedto.Backend.Contracts.PacienteConvenios.Queries;
using Imedto.Backend.Contracts.PacienteConvenios.Queries.Results;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.Domain.PacienteConvenios;
using Imedto.Backend.Infrastructure.Database.Repositories.Convenios;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;
using Imedto.Backend.Infrastructure.Cobrancas;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.Domain.Idempotency;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.Infrastructure.Ia;
using Imedto.Backend.Infrastructure.Jobs;
using Imedto.Backend.Infrastructure.Jobs.Handlers;
using Imedto.Backend.Infrastructure.Automacoes;
using Imedto.Backend.Infrastructure.Notificacoes;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Receitas;
using Imedto.Backend.Infrastructure.Lgpd;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.API.Filters;
using Imedto.Backend.API.Realtime;

namespace Imedto.Backend.API;

/// <summary>
/// Composition Root da API.
///
/// ADICIONAR NOVO DOMÍNIO:
/// 1. Criar arquivos em Domain / Contracts / Application / Infrastructure (EntityTypeConfiguration + DbSet).
/// 2. Registrar handler em RegistrarHandlers (commands/events: Scoped; query handlers: Singleton).
/// 3. Registrar rota no bus em RegistrarBuses.
/// 4. Gerar migration EF + copiar SQL idempotente para db/migrations/.
/// </summary>
public static class Container
{
    public static IServiceCollection Install(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache(); // usado pelo AssinaturaService (gating de feature).

        // Data Protection — chaves persistidas em /var/imedto/dp-keys para sobreviver a deploys.
        // Sem isso, cada restart gera novo key ring e invalida tokens cifrados (ex.: refresh_token do certificado).
        var dpKeysPath = configuration.GetValue<string>("DataProtection:KeysPath")
            ?? "/var/imedto/dp-keys";
        System.IO.Directory.CreateDirectory(dpKeysPath);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new System.IO.DirectoryInfo(dpKeysPath))
            .SetApplicationName("imedto-backend");

        services.AddInfrastructure(configuration);
        RegistrarIa(services, configuration);
        RegistrarHandlers(services);
        RegistrarBuses(services);

        // Assinatura Digital — configuração do provedor BirdID.
        services.Configure<Imedto.Backend.Infrastructure.AssinaturaDigital.BirdIdOptions>(
            configuration.GetSection(Imedto.Backend.Infrastructure.AssinaturaDigital.BirdIdOptions.Section));

        // Admin
        services.AddScoped<Domain.Admin.IAdminResetService, AdminResetService>();
        RegistrarAdminFatias(services);

        // Idempotência
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IdempotencyFilter>();

        // Scheduler de jobs (item 2.1) — BackgroundService nativo + advisory lock para single-leader.
        // Repositório é Scoped (usa AppDbContext); handlers IJobHandler também Scoped — o scheduler
        // resolve via IServiceScopeFactory para obter um DbContext fresco por execução de job.
        services.AddScoped<IJobAgendadoRepository, JobAgendadoRepository>();
        services.AddScoped<IJobHandler, LimparAuditAntigoJob>();
        services.AddScoped<IJobHandler, LimparAuditAdminJob>(); // Wave 7 — retenção audit admin
        services.AddScoped<IJobHandler, ExpirarTrialsJob>(); // item 2.7
        services.AddScoped<IJobHandler, LimparCacheIaJob>(); // item 3.8
        services.AddSingleton<JobScheduler>();
        services.AddHostedService(sp => sp.GetRequiredService<JobScheduler>());

        // Item 2.7 — Seed do catálogo de planos (roda uma vez na startup, idempotente).
        services.AddHostedService<SeedPlanosHostedService>();

        return services;
    }

    private static void RegistrarAdminFatias(IServiceCollection services)
    {
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.AdminQueryRepository>();
        services.AddScoped<Imedto.Backend.Application.Admin.Admins.Queries.ListarAdminsQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Admins.Queries.ObterAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Admins.Commands.CriarAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Admins.Commands.DesativarAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Admins.Commands.ReativarAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Admins.Commands.ResetSenhaAdminCommandHandler>();

        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.IAdminEstabelecimentosQueryRepository,
                              Imedto.Backend.Infrastructure.Admin.AdminEstabelecimentosQueryRepository>();
        services.AddScoped<Imedto.Backend.Application.Admin.Estabelecimentos.Queries.ListarEstabelecimentosAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Estabelecimentos.Queries.ObterEstabelecimentoAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Estabelecimentos.Queries.RevelarCpfDonoQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Estabelecimentos.Commands.ResetTenantCommandHandler>();

        services.AddScoped<Imedto.Backend.Domain.Admin.IImedtoPlanoRepository,
                           Imedto.Backend.Infrastructure.Admin.ImedtoPlanoRepository>();
        services.AddScoped<Imedto.Backend.Domain.Admin.IImedtoAssinaturaRepository,
                           Imedto.Backend.Infrastructure.Admin.ImedtoAssinaturaRepository>();
        services.AddScoped<Imedto.Backend.Domain.Admin.IImedtoConfigTrialRepository,
                           Imedto.Backend.Infrastructure.Admin.ImedtoConfigTrialRepository>();
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.ImedtoPlanoQueryRepository>();
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.ImedtoAssinaturaQueryRepository>();
        services.AddScoped<Imedto.Backend.Application.Admin.Planos.CriarPlanoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Planos.AtualizarPlanoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Planos.AtivarPlanoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Planos.DesativarPlanoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Planos.ListarPlanosAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Planos.ObterPlanoAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Assinaturas.TrocarPlanoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Assinaturas.ConcederGratuidadeAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Assinaturas.EncerrarAssinaturaAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Assinaturas.ListarHistoricoAssinaturasAdminQueryHandler>();

        // Frente 2 — Configs globais
        services.AddSingleton<Imedto.Backend.Domain.Admin.IConfigGlobalReader,
                              Imedto.Backend.Infrastructure.Admin.ConfigGlobalReader>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Configs.ListarConfigsAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Configs.AtualizarConfigAdminCommandHandler>();

        // Wave 4 — Modelos padrão sistema (live-link: EhPadraoSistema=true)
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.QueryRepositories.ModeloPadraoSistemaQueryRepository>();
        services.AddSingleton<Imedto.Backend.Application.Admin.ModelosPadraoSistema.ListarModelosPadraoSistemaQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.ModelosPadraoSistema.ObterModeloPadraoSistemaQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPadraoSistema.CriarModeloPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPadraoSistema.AtualizarModeloPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPadraoSistema.InativarModeloPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPadraoSistema.ReativarModeloPadraoSistemaCommandHandler>();

        // Wave 4 — Variáveis pool padrão sistema
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.QueryRepositories.VariavelPadraoSistemaQueryRepository>();
        services.AddSingleton<Imedto.Backend.Application.Admin.VariaveisPadraoSistema.ListarVariaveisPadraoSistemaQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.VariaveisPadraoSistema.ObterVariavelPadraoSistemaQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.VariaveisPadraoSistema.CriarVariavelPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.VariaveisPadraoSistema.AtualizarVariavelPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.VariaveisPadraoSistema.InativarVariavelPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.VariaveisPadraoSistema.ReativarVariavelPadraoSistemaCommandHandler>();

        // Wave 4 — Regiões anatômicas (catálogo global)
        services.AddScoped<Imedto.Backend.Infrastructure.Admin.RegiaoAnatomicaCatalogoRepository>();
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.QueryRepositories.RegiaoAnatomicaAdminQueryRepository>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Regioes.ListarArvoreRegioesAdminQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Regioes.ObterRegiaoAdminQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Regioes.CriarRegiaoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Regioes.AtualizarRegiaoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Regioes.InativarRegiaoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Regioes.ReativarRegiaoAdminCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.Regioes.ExcluirRegiaoAdminCommandHandler>();

        // Briefing 2026-06-04_001 — Modelos de permissão padrão sistema (cópias materializadas + propagação cross-tenant)
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.QueryRepositories.ModeloPermissaoPadraoSistemaQueryRepository>();
        services.AddSingleton<Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema.ListarModelosPermissaoPadraoSistemaQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema.ObterModeloPermissaoPadraoSistemaQueryHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema.CriarModeloPermissaoPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema.AtualizarModeloPermissaoPadraoSistemaCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Admin.ModelosPermissaoPadraoSistema.ExcluirModeloPermissaoPadraoSistemaCommandHandler>();

        // Wave 6 — Dashboard admin (4 query handlers singleton + repositório Dapper)
        services.AddSingleton<Imedto.Backend.Infrastructure.Admin.QueryRepositories.IDashboardAdminQueryRepository,
                              Imedto.Backend.Infrastructure.Admin.QueryRepositories.DashboardAdminQueryRepository>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Dashboard.ObterKpisDashboardAdminQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Dashboard.ObterCrescimentoMensalDashboardAdminQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Dashboard.ObterAlertasDashboardAdminQueryHandler>();
        services.AddSingleton<Imedto.Backend.Application.Admin.Dashboard.ListarAuditLogDashboardAdminQueryHandler>();
    }

    /// <summary>
    /// Registra o pipeline de IA: serviço concreto Anthropic, decorator com rate
    /// limit/cache/audit e seus repositórios. <see cref="IIaService"/> resolve
    /// sempre para o decorator — quem injeta a interface ganha as proteções de graça.
    /// </summary>
    private static void RegistrarIa(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IaOptions>(configuration.GetSection(IaOptions.Section));

        // Repositórios de apoio
        services.AddScoped<IAiAuditRepository, AiAuditRepository>();
        services.AddScoped<IAiCacheRepository, AiCacheRepository>();
        services.AddScoped<IAiRateLimitRepository, AiRateLimitRepository>();
        services.AddScoped<IEstabelecimentoIaSettingsRepository, EstabelecimentoIaSettingsRepository>();

        // Concreto + decorator (decorator é a única coisa exposta como IIaService)
        services.AddScoped<AnthropicIaService>();
        services.AddScoped<IIaService>(sp => new RateLimitedIaService(
            sp.GetRequiredService<AnthropicIaService>(),
            sp.GetRequiredService<IAiAuditRepository>(),
            sp.GetRequiredService<IAiCacheRepository>(),
            sp.GetRequiredService<IAiRateLimitRepository>(),
            sp.GetRequiredService<IEstabelecimentoIaSettingsRepository>(),
            sp.GetRequiredService<Domain.Vinculos.IVinculoRepository>(),
            sp.GetRequiredService<Domain.ModelosPermissao.IModeloPermissaoRepository>(),
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<IOptions<IaOptions>>(),
            configuration));
    }

    private static void RegistrarHandlers(IServiceCollection services)
    {
        // Auth — bootstrap unificado da SPA (usuário + profissional + estabelecimentos).
        // Scoped: consome IUsuario2faRepository (scoped) — singleton quebra a validação de DI.
        services.AddScoped<BootstrapMeQueryHandlers>();

        // Onboarding
        services.AddScoped<FinalizarOnboardingCommandHandler>();

        // Usuarios
        services.AddSingleton<UsuarioQueryRepository>();
        services.AddScoped<CriarRegistroLocalUsuarioCommandHandler>();
        services.AddScoped<AtualizarPerfilUsuarioCommandHandler>();
        services.AddScoped<CompletarOnboardingUsuarioCommandHandler>();
        services.AddScoped<RegistrarUltimoEstabelecimentoCommandHandler>();
        services.AddSingleton<VerificarCpfDisponivelQueryHandler>();
        services.AddScoped<UsuarioCriadoEventHandler>();

        // Estabelecimentos
        services.AddScoped<CriarEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarFuncionamentoCommandHandler>();
        services.AddScoped<AlterarFotoEstabelecimentoCommandHandler>();
        services.AddScoped<RemoverFotoEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarExigirDono2faCommandHandler>();
        services.AddSingleton<ListarMeusEstabelecimentosQueryHandlers>();
        services.AddSingleton<VerificarCnpjDisponivelQueryHandler>();
        services.AddScoped<EstabelecimentoCriadoEventHandler>();
        services.AddScoped<CriarModeloPadraoAoCriarEstabelecimentoHandler>();
        services.AddScoped<CriarUnidadePadraoAoCriarEstabelecimentoHandler>();
        services.AddScoped<IniciarTrialAoCriarEstabelecimentoHandler>(); // item 2.7
        services.AddSingleton<EstabelecimentoQueryRepository>();

        // Assinaturas (item 2.7) — Plano, Assinatura, gating de feature.
        services.AddScoped<IPlanoRepository, PlanoRepository>();
        services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
        services.AddScoped<IAssinaturaService, AssinaturaService>();
        services.AddSingleton<PlanoQueryRepository>();
        services.AddSingleton<AssinaturaQueryRepository>();
        services.AddSingleton<ListarPlanosQueryHandlers>();
        services.AddSingleton<ObterMinhaAssinaturaQueryHandlers>();

        // Unidades do estabelecimento
        services.AddScoped<CriarUnidadeCommandHandler>();
        services.AddScoped<AtualizarUnidadeCommandHandler>();
        services.AddScoped<DeletarUnidadeCommandHandler>();
        services.AddSingleton<ListarUnidadesQueryHandlers>();
        services.AddSingleton<UnidadeQueryRepository>();

        // Salas/Repartições
        services.AddScoped<CriarSalaCommandHandler>();
        services.AddScoped<AtualizarSalaCommandHandler>();
        services.AddScoped<DeletarSalaCommandHandler>();
        services.AddScoped<DesativarSalaCommandHandler>();
        services.AddScoped<ReativarSalaCommandHandler>();
        services.AddSingleton<ListarSalasQueryHandlers>();
        services.AddSingleton<ListarTiposSalaQueryHandlers>();
        services.AddSingleton<SalaQueryRepository>();

        // Profissionais
        services.AddScoped<CadastrarProfissionalCommandHandler>();
        services.AddScoped<AtualizarProfissionalCommandHandler>();
        services.AddScoped<AlterarFotoProfissionalCommandHandler>();
        services.AddScoped<RemoverFotoProfissionalCommandHandler>();
        services.AddSingleton<ObterProfissionalMeQueryHandlers>();
        services.AddScoped<ProfissionalCadastradoEventHandler>();
        services.AddSingleton<ProfissionalQueryRepository>();

        // Vínculos
        services.AddScoped<ConvidarProfissionalCommandHandler>();
        services.AddScoped<AceitarConviteCommandHandler>();
        services.AddScoped<AceitarConvitesPendentesDoUsuarioCommandHandler>();
        services.AddScoped<InativarVinculoCommandHandler>();
        services.AddScoped<ReativarVinculoCommandHandler>();
        services.AddScoped<ReenviarConviteCommandHandler>();
        services.AddScoped<AlterarModeloPermissaoDoVinculoCommandHandler>();
        services.AddScoped<AlterarEspecialidadeDoVinculoCommandHandler>();
        services.AddScoped<AlterarProfissaoEspecialidadeDoVinculoCommandHandler>();
        services.AddScoped<ListarProfissionaisEstabelecimentoQueryHandlers>();
        services.AddSingleton<ListarProfissionaisPublicoQueryHandlers>();
        services.AddSingleton<ListarMeusConvitesQueryHandlers>();
        services.AddScoped<ProfissionalConvidadoEventHandler>();
        services.AddScoped<VinculoAceitoEventHandler>();
        services.AddSingleton<VinculoQueryRepository>();
        // Item 4.2 — Solicitação de vínculo inversa (profissional → estabelecimento).
        services.AddScoped<SolicitarVinculoCommandHandler>();
        services.AddScoped<AprovarSolicitacaoVinculoCommandHandler>();
        services.AddScoped<RecusarSolicitacaoVinculoCommandHandler>();
        services.AddScoped<CancelarSolicitacaoVinculoCommandHandler>();
        services.AddSingleton<ListarMinhasSolicitacoesVinculoQueryHandlers>();
        services.AddScoped<ListarSolicitacoesVinculoRecebidasQueryHandlers>();
        services.AddScoped<AoAprovarSolicitacaoCriarVinculoHandler>();
        services.AddScoped<NotificarSolicitacaoCriadaHandler>();
        services.AddScoped<NotificarSolicitacaoRespondidaHandler>();
        services.AddSingleton<SolicitacaoVinculoQueryRepository>();

        // Pacientes
        services.AddScoped<CadastrarPacienteCommandHandler>();
        services.AddScoped<AtualizarPacienteCommandHandler>();
        services.AddScoped<DeletarPacienteCommandHandler>();
        services.AddSingleton<ListarPacientesQueryHandlers>();
        services.AddSingleton<BuscaRapidaPacientesQueryHandler>();
        services.AddSingleton<ObterPacienteStatsQueryHandler>();
        // Scoped: Obter/Export auditam acesso via IPacienteAcessoLogService (LGPD).
        services.AddScoped<ObterPacienteQueryHandlers>();
        services.AddScoped<ExportarDadosPacienteQueryHandlers>();
        // Scoped: ListarDocumentos audita leitura ao prontuário via IProntuarioAcessoLogService.
        services.AddScoped<IDocumentoQueryRepository, DocumentoQueryRepository>();
        services.AddScoped<ListarDocumentosDoPacienteQueryHandlers>();
        // Scoped: ListarAcessos audita a própria consulta via IPacienteAcessoLogService (R4/CA10).
        services.AddScoped<IAcessoQueryRepository, AcessoQueryRepository>();
        services.AddScoped<ListarAcessosDoPacienteQueryHandlers>();
        services.AddScoped<PacienteCadastradoEventHandler>();
        services.AddSingleton<PacienteQueryRepository>();

        // Prontuários (templates + pool)
        services.AddScoped<CriarModeloDeProntuarioCommandHandler>();
        services.AddScoped<AtualizarModeloDeProntuarioCommandHandler>();
        services.AddScoped<ExcluirModeloDeProntuarioCommandHandler>();
        services.AddScoped<AdicionarVariavelPoolCommandHandler>();
        services.AddScoped<AtualizarVariavelPoolCommandHandler>();
        services.AddScoped<ExcluirVariavelPoolCommandHandler>();
        services.AddSingleton<ListarModelosDisponiveisQueryHandlers>();
        services.AddSingleton<ObterModeloDeProntuarioQueryHandlers>();
        services.AddSingleton<ListarVariaveisPoolQueryHandlers>();
        services.AddSingleton<ModeloProntuarioQueryRepository>();
        services.AddSingleton<VariavelPoolQueryRepository>();

        // Prontuários (aggregate + evoluções)
        services.AddScoped<PoolExtratorEvolucao>(); // extração automática de itens do pool ao salvar evolução
        // F3B — Pendências de atendimento (briefing 2026-06-10_012).
        // IPendenciaAtendimentoRepository (scoped, EF) registrado em Infrastructure/Container.cs.
        services.AddSingleton<PendenciaQueryRepository>(); // Dapper singleton — padrão dos outros *QueryRepository
        services.AddScoped<PendenciaExtratorEvolucao>(); // extração de ações de conduta → pendências
        services.AddScoped<ConcluirPendenciaManualCommandHandler>();
        services.AddScoped<MarcarProcedimentoRealizadoCommandHandler>(); // F4 — 2026-06-10_013
        services.AddSingleton<ListarPendenciasAbertasQueryHandler>();
        services.AddScoped<PreviewProcedimentoRealizadoQueryHandler>(); // F4 — consome IPendenciaAtendimentoRepository (scoped)
        services.AddSingleton<ObterProcedimentosIndicadosQueryHandler>(); // F5 — pré-preenchimento form de orçamento
        // Handlers de conclusão automática (ouvem eventos existentes — R7-R11)
        services.AddScoped<ConcluirPendenciaAoEmitirReceitaHandler>();
        services.AddScoped<ConcluirPendenciaAoEmitirAtestadoHandler>();
        services.AddScoped<ConcluirPendenciaAoEmitirPedidoExameHandler>();
        services.AddScoped<ConcluirPendenciaAoCriarOrcamentoHandler>();
        services.AddScoped<ConcluirPendenciaAoCriarAgendamentoHandler>();
        services.AddScoped<IniciarProntuarioCommandHandler>();
        services.AddScoped<RegistrarEvolucaoCommandHandler>();
        services.AddScoped<RegistrarExportacaoProntuarioCommandHandler>();
        services.AddScoped<RegistrarExportacaoEvolucaoCommandHandler>();
        services.AddScoped<ObterProntuarioDoPacienteQueryHandlers>(); // scoped — injeta IProntuarioAcessoLogService (scoped)
        services.AddScoped<ListarEvolucoesProntuarioPacienteQueryHandlers>(); // scoped — injeta IProntuarioAcessoLogService (scoped)
        services.AddSingleton<ContarEvolucoesProntuarioPacienteQueryHandlers>(); // só COUNT, sem audit — pode ser singleton
        services.AddSingleton<ProntuarioQueryRepository>();
        services.AddScoped<ProntuarioIniciadoEventHandler>();
        services.AddScoped<EvolucaoRegistradaEventHandler>();

        // Receitas (item 3.1)
        services.AddScoped<IReceitaRepository, ReceitaRepository>();
        services.AddScoped<IConfiguracaoReceitaRepository, ConfiguracaoReceitaRepository>();
        services.AddScoped<IMedicamentoFavoritoRepository, MedicamentoFavoritoRepository>();
        // Scoped (vs Singleton dos outros *QueryRepository) para alinhar com os handlers
        // de Receita que sao Scoped por causa do IProntuarioAcessoLogService (audit LGPD).
        services.AddScoped<IReceitaQueryRepository, ReceitaQueryRepository>();
        services.AddScoped<IReceitaPdfService, QuestPdfReceitaService>(); // placeholder — Wave 4
        services.AddScoped<EmitirReceitaCommandHandler>();
        services.AddScoped<CancelarReceitaCommandHandler>();
        services.AddScoped<DuplicarReceitaCommandHandler>();
        services.AddScoped<IniciarRascunhoReceitaCommandHandler>();
        services.AddScoped<AtualizarRascunhoReceitaCommandHandler>();
        services.AddScoped<FinalizarReceitaCommandHandler>();
        services.AddScoped<AtualizarConfiguracaoReceitaCommandHandler>();
        // Query handlers de receita são SCOPED — dependem de IProntuarioAcessoLogService (audit LGPD).
        services.AddScoped<ListarReceitasDoPacienteQueryHandlers>();
        services.AddScoped<ObterReceitaQueryHandlers>();
        services.AddScoped<ObterConfiguracaoReceitaQueryHandlers>();

        // Atestados (2026-05-18)
        services.AddScoped<IAtestadoRepository, AtestadoRepository>();
        services.AddScoped<IModeloAtestadoRepository, ModeloAtestadoRepository>();
        services.AddScoped<IAtestadoQueryRepository, AtestadoQueryRepository>();
        services.AddScoped<EmitirAtestadoCommandHandler>();
        services.AddScoped<CriarModeloAtestadoCommandHandler>();
        services.AddScoped<AtualizarModeloAtestadoCommandHandler>();
        services.AddScoped<ExcluirModeloAtestadoCommandHandler>();
        services.AddScoped<ListarAtestadosDoPacienteQueryHandlers>();
        services.AddScoped<ObterAtestadoQueryHandlers>();
        services.AddScoped<ListarModelosAtestadoQueryHandlers>();

        // Pedidos de exame (2026-05-18)
        services.AddScoped<IPedidoExameRepository, PedidoExameRepository>();
        services.AddScoped<IPedidoExameQueryRepository, PedidoExameQueryRepository>();
        services.AddScoped<EmitirPedidoExameCommandHandler>();
        services.AddScoped<ListarPedidosExameDoPacienteQueryHandlers>();
        services.AddScoped<ObterPedidoExameQueryHandlers>();

        // Termos de consentimento (Fase 1 — 2026-05-19).
        // Sanitizer e Resolver são stateless após o ctor → singleton.
        services.AddSingleton<ITermoHtmlSanitizer, GanssHtmlSanitizer>();
        services.AddSingleton<ITermoTextoExtractor, SimpleTermoTextoExtractor>();
        services.AddSingleton<ITermoResolverDeVariaveis, TermoResolverDeVariaveis>();
        services.AddScoped<ITermoModeloRepository, TermoModeloRepository>();
        services.AddScoped<ITermoEmitidoRepository, TermoEmitidoRepository>();
        services.AddSingleton<ITermoModeloQueryRepository, TermoModeloQueryRepository>();
        services.AddSingleton<ITermoEmitidoQueryRepository, TermoEmitidoQueryRepository>();
        services.AddScoped<ITermoAuditLogger, EfTermoAuditLogger>();
        services.AddScoped<ITermoPdfStorageService, S3TermoPdfStorageService>();
        // Briefing 2026-06-10_002 — PDF probatório gerado no servidor via QuestPDF.
        services.AddScoped<ITermoPdfGeradoService, QuestPdfTermoService>();
        services.AddScoped<CriarModeloTermoCommandHandler>();
        services.AddScoped<AtualizarModeloTermoCommandHandler>();
        services.AddScoped<AlterarAtivoModeloTermoCommandHandler>();
        services.AddScoped<ExcluirModeloTermoCommandHandler>();
        services.AddScoped<ClonarModeloTermoCommandHandler>();
        services.AddScoped<EmitirTermoCommandHandler>();
        services.AddScoped<AnexarPdfTermoCommandHandler>();
        services.AddScoped<RevogarTermoCommandHandler>();
        // Fase 4 — aceite público + reenvio de link + notificações por e-mail.
        services.AddScoped<RegistrarRespostaPublicaTermoCommandHandler>();
        services.AddScoped<ReenviarLinkTermoCommandHandler>();
        services.AddScoped<Imedto.Backend.Application.Termos.Events.EnviarEmailTermoLinkEventHandler>();
        services.AddScoped<Imedto.Backend.Application.Termos.Events.NotificarEmissorTermoAssinadoEventHandler>();
        services.AddScoped<Imedto.Backend.Application.Termos.Events.NotificarEmissorTermoRecusadoEventHandler>();
        // Query handlers que auditam acesso ou dependem do DbContext via repo são Scoped.
        services.AddSingleton<ListarModelosTermoQueryHandlers>();
        services.AddSingleton<ListarModelosPadraoTermoQueryHandlers>();
        services.AddSingleton<ObterModeloTermoQueryHandlers>();
        services.AddSingleton<ListarVariaveisDisponiveisQueryHandlers>();
        services.AddScoped<ListarTermosDoPacienteQueryHandlers>();
        services.AddScoped<ObterTermoEmitidoQueryHandlers>();
        services.AddScoped<ObterUrlPdfTermoQueryHandlers>();
        // Fase 4 — fluxo público anônimo via token.
        services.AddScoped<ObterTermoPublicoPorTokenQueryHandler>();

        // Modelos de Permissão
        services.AddScoped<CriarModeloPermissaoCommandHandler>();
        services.AddScoped<AtualizarModeloPermissaoCommandHandler>();
        services.AddScoped<ExcluirModeloPermissaoCommandHandler>();
        services.AddSingleton<ListarModelosPermissaoQueryHandlers>();
        services.AddSingleton<ModeloPermissaoQueryRepository>();

        // Agendamentos
        services.AddScoped<CriarAgendamentoCommandHandler>();
        services.AddScoped<AtualizarAgendamentoCommandHandler>();
        services.AddScoped<CancelarAgendamentoCommandHandler>();
        services.AddScoped<ConfirmarAgendamentoCommandHandler>();
        services.AddScoped<ConcluirAgendamentoCommandHandler>();
        services.AddScoped<RegistrarCheckInAgendamentoCommandHandler>();
        services.AddScoped<AlocarSalaAgendamentoCommandHandler>();
        services.AddScoped<IListaEsperaRepository, ListaEsperaRepository>();
        services.AddSingleton<ListaEsperaQueryRepository>();
        services.AddScoped<AdicionarListaEsperaCommandHandler>();
        services.AddScoped<RemoverListaEsperaCommandHandler>();
        services.AddSingleton<ListarListaEsperaQueryHandler>();
        services.AddSingleton<ListarAgendamentosQueryHandlers>();
        services.AddSingleton<ContarAgendamentosPorDiaQueryHandler>();
        services.AddSingleton<ObterAgendamentoQueryHandlers>();
        services.AddSingleton<ConsultarDisponibilidadeQueryHandlers>();
        services.AddSingleton<AgendamentoQueryRepository>();
        services.AddScoped<AgendamentoCriadoEventHandler>();
        services.AddScoped<AgendamentoCanceladoEventHandler>();
        services.AddScoped<EnviarEmailAgendamentoReagendadoEventHandler>();
        // Fase 2 — link público de confirmação de presença.
        // Scoped (não singleton): handler usa IAgendamentoRepository (scoped) para gravar log de acesso.
        services.AddScoped<ConfirmarPresencaPublicaCommandHandler>();
        services.AddScoped<ConsultarConfirmacaoPublicaQueryHandler>();

        // Anexos de prontuário
        services.AddScoped<AdicionarAnexoCommandHandler>();
        services.AddScoped<ListarAnexosDoProntuarioQueryHandlers>();
        services.AddScoped<ObterUrlAnexoQueryHandlers>();
        services.AddSingleton<ProntuarioAnexoQueryRepository>();

        // Exame físico (item 3.2)
        services.AddScoped<RegistrarExameFisicoCommandHandler>();
        services.AddScoped<AtualizarExameFisicoCommandHandler>();
        // Query handlers de exame físico são SCOPED — auditam acesso via IProntuarioAcessoLogService.
        services.AddScoped<ObterExameFisicoQueryHandlers>();

        // Inventário
        services.AddScoped<CriarItemInventarioCommandHandler>();
        services.AddScoped<AtualizarItemInventarioCommandHandler>();
        services.AddScoped<RegistrarMovimentacaoEstoqueCommandHandler>();
        services.AddScoped<InativarItemInventarioCommandHandler>();
        services.AddSingleton<ListarItensInventarioQueryHandlers>();
        services.AddSingleton<ListarMovimentacoesQueryHandlers>();
        services.AddSingleton<InventarioQueryRepository>();
        services.AddScoped<EstoqueAbaixoMinimoEventHandler>();

        // Inventário — cadastros mestre (4 aggregates × 4 commands + 4 query handlers).
        services.AddScoped<CriarCategoriaEstoqueCommandHandler>();
        services.AddScoped<AtualizarCategoriaEstoqueCommandHandler>();
        services.AddScoped<InativarCategoriaEstoqueCommandHandler>();
        services.AddScoped<ReativarCategoriaEstoqueCommandHandler>();
        services.AddScoped<CriarFabricanteEstoqueCommandHandler>();
        services.AddScoped<AtualizarFabricanteEstoqueCommandHandler>();
        services.AddScoped<InativarFabricanteEstoqueCommandHandler>();
        services.AddScoped<ReativarFabricanteEstoqueCommandHandler>();
        services.AddScoped<CriarFornecedorEstoqueCommandHandler>();
        services.AddScoped<AtualizarFornecedorEstoqueCommandHandler>();
        services.AddScoped<InativarFornecedorEstoqueCommandHandler>();
        services.AddScoped<ReativarFornecedorEstoqueCommandHandler>();
        services.AddScoped<CriarLocalEstoqueCommandHandler>();
        services.AddScoped<AtualizarLocalEstoqueCommandHandler>();
        services.AddScoped<InativarLocalEstoqueCommandHandler>();
        services.AddScoped<ReativarLocalEstoqueCommandHandler>();
        services.AddSingleton<ListarCategoriasEstoqueQueryHandlers>();
        services.AddSingleton<ListarFabricantesEstoqueQueryHandlers>();
        services.AddSingleton<ListarFornecedoresEstoqueQueryHandlers>();
        services.AddSingleton<ListarLocaisEstoqueQueryHandlers>();
        services.AddSingleton<ObterOpcoesCategoriasEstoqueQueryHandlers>();
        services.AddSingleton<ObterOpcoesFabricantesEstoqueQueryHandlers>();
        services.AddSingleton<ObterOpcoesFornecedoresEstoqueQueryHandlers>();
        services.AddSingleton<ObterOpcoesLocaisEstoqueQueryHandlers>();
        services.AddSingleton<Imedto.Backend.Infrastructure.Database.Repositories.Cadastros.CadastrosEstoqueQueryRepository>();

        // Orçamentos (aggregate único — sem distinção simples/completo).
        services.AddScoped<CriarOrcamentoCommandHandler>();
        services.AddScoped<AtualizarOrcamentoCommandHandler>();
        services.AddScoped<EnviarOrcamentoCommandHandler>();
        services.AddScoped<AprovarOrcamentoCommandHandler>();
        services.AddScoped<RecusarOrcamentoCommandHandler>();
        services.AddScoped<CancelarOrcamentoCommandHandler>();
        services.AddScoped<ConverterOrcamentoEmCirurgiaCommandHandler>();
        services.AddSingleton<ListarOrcamentosQueryHandlers>();
        services.AddSingleton<ObterOrcamentoQueryHandlers>();
        services.AddSingleton<PreviewOrcamentoQueryHandler>();
        services.AddSingleton<ObterOrcamentoPorAgendamentoQueryHandler>();
        services.AddSingleton<ConsolidarProdutosOrcamentoQueryHandler>();
        services.AddSingleton<OrcamentoQueryRepository>();
        services.AddScoped<OrcamentoCriadoEventHandler>();
        services.AddScoped<OrcamentoAprovadoEventHandler>();

        // Fase 6.1 — Catálogos de orçamento (settings).
        services.AddScoped<ICatalogoCirurgiaRepository, CatalogoCirurgiaRepository>();
        services.AddScoped<IValorProfissionalOrcamentoRepository, ValorProfissionalOrcamentoRepository>();
        services.AddScoped<IConfiguracaoLocalCirurgiaRepository, ConfiguracaoLocalCirurgiaRepository>();
        services.AddScoped<ICatalogoEquipeEspecializadaRepository, CatalogoEquipeEspecializadaRepository>();
        services.AddScoped<ICatalogoImplanteRepository, CatalogoImplanteRepository>();
        services.AddScoped<IConfiguracaoPagamentoCatalogoRepository, ConfiguracaoPagamentoCatalogoRepository>();
        services.AddScoped<ICatalogoProdutoRepository, CatalogoProdutoRepository>();
        services.AddScoped<ICatalogoCirurgiaProdutoRepository, CatalogoCirurgiaProdutoRepository>();
        services.AddSingleton<OrcamentoCatalogoQueryRepository>();
        services.AddScoped<CriarCatalogoCirurgiaCommandHandler>();
        services.AddScoped<AtualizarCatalogoCirurgiaCommandHandler>();
        services.AddScoped<RemoverCatalogoCirurgiaCommandHandler>();
        services.AddScoped<CriarValorProfissionalCommandHandler>();
        services.AddScoped<AtualizarValorProfissionalCommandHandler>();
        services.AddScoped<RemoverValorProfissionalCommandHandler>();
        services.AddScoped<SalvarConfiguracaoLocalCommandHandler>();
        services.AddScoped<CriarCatalogoEquipeCommandHandler>();
        services.AddScoped<AtualizarCatalogoEquipeCommandHandler>();
        services.AddScoped<RemoverCatalogoEquipeCommandHandler>();
        services.AddScoped<CriarCatalogoImplanteCommandHandler>();
        services.AddScoped<AtualizarCatalogoImplanteCommandHandler>();
        services.AddScoped<RemoverCatalogoImplanteCommandHandler>();
        services.AddScoped<CriarConfiguracaoPagamentoCommandHandler>();
        services.AddScoped<AtualizarConfiguracaoPagamentoCommandHandler>();
        services.AddScoped<RemoverConfiguracaoPagamentoCommandHandler>();
        services.AddScoped<CriarCatalogoProdutoCommandHandler>();
        services.AddScoped<AtualizarCatalogoProdutoCommandHandler>();
        services.AddScoped<RemoverCatalogoProdutoCommandHandler>();
        services.AddScoped<VincularProdutoCirurgiaCommandHandler>();
        services.AddScoped<AtualizarVinculoProdutoCirurgiaCommandHandler>();
        services.AddScoped<DesvincularProdutoCirurgiaCommandHandler>();
        services.AddSingleton<ListarCatalogoCirurgiasQueryHandlers>();
        services.AddSingleton<ListarValoresProfissionalQueryHandlers>();
        services.AddSingleton<ListarConfiguracoesLocalQueryHandlers>();
        services.AddSingleton<ListarCatalogoEquipesQueryHandlers>();
        services.AddSingleton<ListarCatalogoImplantesQueryHandlers>();
        services.AddSingleton<ListarConfiguracoesPagamentoQueryHandlers>();
        services.AddSingleton<ListarCatalogoProdutosQueryHandlers>();
        services.AddSingleton<ListarProdutosDaCirurgiaQueryHandlers>();
        // Config-orcamento 2026-05-16 — Team Roles, Anestesistas, Pacotes.
        services.AddScoped<IOrcamentoTeamRoleRepository, OrcamentoTeamRoleRepository>();
        services.AddScoped<IOrcamentoAnestesistaRepository, OrcamentoAnestesistaRepository>();
        services.AddScoped<IOrcamentoPacoteRepository, OrcamentoPacoteRepository>();
        services.AddScoped<CriarOrcamentoTeamRoleCommandHandler>();
        services.AddScoped<AtualizarOrcamentoTeamRoleCommandHandler>();
        services.AddScoped<RemoverOrcamentoTeamRoleCommandHandler>();
        services.AddScoped<CriarOrcamentoAnestesistaCommandHandler>();
        services.AddScoped<AtualizarOrcamentoAnestesistaCommandHandler>();
        services.AddScoped<RemoverOrcamentoAnestesistaCommandHandler>();
        services.AddScoped<CriarOrcamentoPacoteCommandHandler>();
        services.AddScoped<AtualizarOrcamentoPacoteCommandHandler>();
        services.AddScoped<RemoverOrcamentoPacoteCommandHandler>();
        services.AddSingleton<ListarOrcamentoTeamRolesQueryHandlers>();
        services.AddSingleton<ListarOrcamentoAnestesistasQueryHandlers>();
        services.AddSingleton<ObterOrcamentoAnestesistaQueryHandlers>();
        services.AddSingleton<ListarOrcamentoPacotesQueryHandlers>();
        services.AddSingleton<ObterOrcamentoPacoteQueryHandlers>();


        // Item 3.3.A — Procedimentos cirúrgicos. Repositório de escrita registrado em
        // Infrastructure.Container (junto com os outros do domínio).
        services.AddScoped<PlanejarProcedimentoCommandHandler>();
        services.AddScoped<ConfirmarProcedimentoCommandHandler>();
        services.AddScoped<RegistrarRealizacaoCommandHandler>();
        services.AddScoped<CancelarProcedimentoCommandHandler>();
        services.AddScoped<AtualizarEquipeCommandHandler>();
        // Query handlers de procedimento são SCOPED — auditam acesso de leitura via IProntuarioAcessoLogService.
        services.AddScoped<ObterProcedimentoQueryHandlers>();
        services.AddSingleton<ListarProcedimentosPlanejadosQueryHandlers>();
        services.AddSingleton<ProcedimentoCirurgicoQueryRepository>();
        // Handler de evento — notifica equipe ao confirmar procedimento.
        services.AddScoped<NotificarEquipeAoConfirmarHandler>();

        // Financeiro — lançamentos
        services.AddScoped<CriarLancamentoCommandHandler>();
        services.AddScoped<AtualizarLancamentoCommandHandler>();
        services.AddScoped<PagarLancamentoCommandHandler>();
        services.AddScoped<CancelarLancamentoCommandHandler>();
        services.AddSingleton<ListarLancamentosQueryHandlers>();
        services.AddSingleton<ObterResumoFinanceiroQueryHandlers>();
        services.AddSingleton<FinanceiroQueryRepository>();
        services.AddScoped<LancamentoCriadoEventHandler>();
        services.AddScoped<LancamentoPagoEventHandler>();

        // Financeiro — categorias e formas de pagamento (item 2.10)
        services.AddScoped<ICategoriaFinanceiraRepository, CategoriaFinanceiraRepository>();
        services.AddScoped<IFormaPagamentoRepository, FormaPagamentoRepository>();
        services.AddScoped<CriarCategoriaFinanceiraCommandHandler>();
        services.AddScoped<AtualizarCategoriaFinanceiraCommandHandler>();
        services.AddScoped<InativarCategoriaFinanceiraCommandHandler>();
        services.AddScoped<CriarFormaPagamentoCommandHandler>();
        services.AddScoped<AtualizarFormaPagamentoCommandHandler>();
        services.AddScoped<InativarFormaPagamentoCommandHandler>();
        services.AddSingleton<ListarCategoriasFinanceirasQueryHandlers>();
        services.AddSingleton<ListarFormasPagamentoQueryHandlers>();
        services.AddSingleton<CategoriaFinanceiraQueryRepository>();
        services.AddSingleton<FormaPagamentoQueryRepository>();
        services.AddScoped<CriarSeedFinanceiroAoCriarEstabelecimentoHandler>();

        // Automações — configurações legadas (lembretes/orçamentos vencidos)
        services.AddScoped<ExpirarOrcamentosVencidosCommandHandler>();
        services.AddScoped<EnviarLembretesAgendamentosCommandHandler>();
        services.AddScoped<SalvarConfiguracaoAutomacaoCommandHandler>();
        services.AddSingleton<ObterConfiguracaoAutomacaoQueryHandlers>();
        services.AddScoped<IConfiguracaoAutomacaoRepository, ConfiguracaoAutomacaoRepository>();
        // E-mail provider configurável via Email:Provider:
        //   - "Ses"    → AWS SES v2 (free tier 62k/mês via EC2; preferido em prod)
        //   - "Resend" → Resend HTTP API (default; sem sandbox, melhor pra dev)
        //   - vazio    → NoOp (loga, não envia)
        services.AddSingleton<Amazon.SimpleEmailV2.IAmazonSimpleEmailServiceV2>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var region = Amazon.RegionEndpoint.GetBySystemName(
                cfg["Email:Ses:Region"] ?? cfg["Storage:Region"] ?? "sa-east-1");
            return new Amazon.SimpleEmailV2.AmazonSimpleEmailServiceV2Client(region);
        });

        services.AddScoped<IEmailService>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var provider = (cfg["Email:Provider"] ?? "").Trim().ToLowerInvariant();

            return provider switch
            {
                "ses" => ActivatorUtilities.CreateInstance<SesEmailService>(sp),
                "resend" => ResolverResendOuNoOp(sp, cfg),
                _ => ResolverResendOuNoOp(sp, cfg) // default: tenta Resend, fallback NoOp
            };

            static IEmailService ResolverResendOuNoOp(IServiceProvider sp, IConfiguration cfg)
            {
                var apiKey = cfg["Email:ApiKey"] ?? cfg["Email:ResendApiKey"];
                return string.IsNullOrWhiteSpace(apiKey)
                    ? ActivatorUtilities.CreateInstance<NoOpEmailService>(sp)
                    : ActivatorUtilities.CreateInstance<ResendEmailService>(sp);
            }
        });

        // Item 2.2 — Engine de automações (regras + worker + executor)
        services.AddScoped<IRegraAutomacaoRepository, RegraAutomacaoRepository>();
        services.AddScoped<IEventoAutomacaoRepository, EventoAutomacaoRepository>();
        services.AddScoped<IExecutorAcao, ExecutorAcao>();
        services.AddScoped<CriarRegraAutomacaoCommandHandler>();
        services.AddScoped<AtualizarRegraAutomacaoCommandHandler>();
        services.AddScoped<AtivarRegraAutomacaoCommandHandler>();
        services.AddScoped<DesativarRegraAutomacaoCommandHandler>();
        services.AddScoped<ListarRegrasAutomacaoQueryHandlers>();
        services.AddScoped<ListarEventosAutomacaoQueryHandlers>();
        // Handlers de enfileiramento (ouvem eventos de domínio e criam EventoAutomacao)
        services.AddScoped<EnfileirarAutomacaoAgendamentoCriadoHandler>();
        services.AddScoped<EnfileirarAutomacaoOrcamentoAprovadoHandler>();
        services.AddScoped<EnfileirarAutomacaoLancamentoCriadoHandler>();
        // Worker registrado como IJobHandler — scheduler resolve por Nome.
        services.AddScoped<IJobHandler, ProcessadorAutomacoesJob>();

        // Item 2.3 — Notificações in-app
        services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
        services.AddScoped<INotificacaoService, NotificacaoService>();
        services.AddSingleton<NotificacaoQueryRepository>();
        services.AddScoped<MarcarNotificacaoLidaCommandHandler>();
        services.AddScoped<MarcarTodasNotificacoesLidasCommandHandler>();
        services.AddScoped<ListarNotificacoesQueryHandlers>();
        services.AddScoped<ContadorNaoLidasQueryHandlers>();
        // Handler de domínio: converte ProfissionalConvidadoEvent em notificação in-app.
        services.AddScoped<NotificarConviteAoConvidarProfissionalHandler>();
        // Item 2.4 — Bridge SignalR: empurra NotificacaoCriadaEvent para o cliente conectado.
        services.AddScoped<NotificacaoCriadaSignalRBridge>();
        // Permissões alteradas → push em tempo real para revalidação no cliente.
        services.AddScoped<PermissoesAlteradasSignalRBridge>();
        // IIaService registrado via RegistrarIa (decorator com rate limit + cache + audit).

        // Catálogo (Profissões, Especialidades e Regiões Anatômicas)
        services.AddSingleton<ListarProfissoesQueryHandlers>();
        services.AddSingleton<ListarEspecialidadesQueryHandlers>();
        services.AddSingleton<ListarRegioesCatalogoQueryHandlers>();
        services.AddSingleton<CatalogoQueryRepository>();

        // Item 4.13 — Catálogo TUSS/CBHPM de procedimentos.
        services.AddScoped<IProcedimentoCatalogoRepository, ProcedimentoCatalogoRepository>();
        services.AddSingleton<ProcedimentoCatalogoQueryRepository>();
        services.AddSingleton<BuscarProcedimentoCatalogoQueryHandlers>();
        services.AddSingleton<ObterProcedimentoPorCodigoQueryHandlers>();

        // Assinatura Digital ICP-Brasil (2026-06-01).
        // (Configure<BirdIdOptions> feito em Install() que tem acesso à configuration)
        services.AddScoped<IAssinaturaCertificadoRepository, AssinaturaCertificadoRepository>();
        services.AddScoped<IAssinaturaAuditLogRepository, AssinaturaAuditLogRepository>();
        services.AddSingleton<IAssinaturaDigitalQueryRepository, AssinaturaDigitalQueryRepository>();
        services.AddSingleton<IAssinaturaDigitalProvider, Imedto.Backend.Infrastructure.AssinaturaDigital.BirdIdAssinaturaProvider>();
        services.AddScoped<VincularCertificadoCommandHandler>();
        services.AddScoped<RemoverCertificadoCommandHandler>();
        services.AddScoped<DispararAssinaturaCommandHandler>();
        services.AddScoped<ProcessarCallbackAssinaturaCommandHandler>();
        services.AddScoped<ExpirarAssinaturasPendentesCommandHandler>();
        services.AddScoped<ObterStatusAssinaturaQueryHandler>();
        services.AddScoped<ObterCertificadoVinculadoQueryHandler>();
        services.AddScoped<IJobHandler, Imedto.Backend.Infrastructure.AssinaturaDigital.ExpirarAssinaturasPendentesJob>();

        // Item 4.3 — LGPD: anonimização e exportação de dados do titular.
        services.AddScoped<ILgpdAnonimizacaoRepository, LgpdAnonimizacaoRepository>();
        services.AddScoped<IAnonimizacaoService, AnonimizacaoService>();
        services.AddSingleton<LgpdQueryRepository>();
        services.AddScoped<AnonimizarMinhaContaCommandHandler>();
        services.AddSingleton<ExportarMeusDadosLgpdQueryHandlers>();
        services.AddScoped<IJobHandler, AnonimizarPacientesInativosJob>();

        // Dashboard & Relatórios
        services.AddSingleton<DashboardQueryHandlers>();
        services.AddSingleton<DashboardQueryRepository>();
        services.AddSingleton<RelatorioFaturamentoQueryHandlers>();
        services.AddSingleton<RelatorioAgendamentosQueryHandlers>();
        // Item 4.1 — relatórios consolidados (4 handlers cobrem 9 RPCs legados).
        services.AddSingleton<RelatorioFinanceiroQueryHandler>();
        services.AddSingleton<RelatorioOperacionalQueryHandler>();
        services.AddSingleton<RelatorioPessoasQueryHandler>();
        services.AddSingleton<RelatorioOrcamentosQueryHandler>();
        services.AddSingleton<RelatorioQueryRepository>();

        // Cobranças F1/F2 — repositórios + handlers
        // Query repo singleton (Dapper — sem estado por request)
        services.AddSingleton<CobrancaQueryRepository>();
        // Commands scoped (escrita via EF — mesma transação UoW)
        services.AddScoped<RegistrarPagamentosCommandHandler>();
        services.AddScoped<EstornarPagamentoCommandHandler>();
        services.AddScoped<SalvarTabelaPrecoConsultaCommandHandler>();
        services.AddScoped<InativarTabelaPrecoConsultaCommandHandler>();
        services.AddScoped<SalvarConfigTaxaFormaPagamentoCommandHandler>();
        // F6: guia de autorização
        services.AddScoped<RegistrarGuiaCobrancaCommandHandler>();
        // Query handlers singleton (leitura Dapper) — exceto ObterFinanceiroAba que audita acesso (scoped)
        services.AddSingleton<ObterCobrancaDaAgendaQueryHandlers>();
        services.AddSingleton<ObterValorSugeridoCheckInQueryHandlers>();
        services.AddSingleton<ListarTabelaPrecoConsultaQueryHandlers>();
        services.AddSingleton<ListarConfigTaxaFormaPagamentoQueryHandlers>();
        // F2: scoped — injeta IPacienteAcessoLogService (scoped, LGPD audit)
        services.AddScoped<ObterFinanceiroAbaQueryHandler>();
        // F8: recibo de pagamento — scoped (persiste recibo_emitido_em via ICobrancaRepository)
        services.AddScoped<IReciboPagamentoPdfService, QuestPdfReciboPagamentoService>();
        services.AddScoped<EmitirReciboPagamentoQueryHandler>();

        // F7 — Consolidação Financeira (2026-06-11_001)
        // Write handlers (scoped — EF Core)
        services.AddScoped<AbrirCaixaDiarioCommandHandler>();
        services.AddScoped<FecharCaixaDiarioCommandHandler>();
        services.AddScoped<ReabrirCaixaDiarioCommandHandler>();
        services.AddScoped<SalvarComissaoProfissionalCommandHandler>();
        // Query handlers (singleton — Dapper read-only via ConsolidacaoFinanceiraQueryRepository)
        services.AddSingleton<ObterKpisFinanceiroQueryHandler>();
        services.AddSingleton<ListarExtratoQueryHandler>();
        services.AddSingleton<ObterCaixaDiarioQueryHandler>();
        services.AddSingleton<ObterComissoesPeriodoQueryHandler>();
        services.AddSingleton<ObterConfigComissaoQueryHandler>();
        // F7 redesign — Export de extrato com audit LGPD (briefing 2026-06-11_002)
        services.AddSingleton<ExportarExtratoQueryHandler>();

        // F6 — Convênios: estrutura base (briefing 2026-06-10_016)
        // Repositórios de escrita registrados em Infrastructure/Container.cs (junto com o CheckInHandler)
        services.AddSingleton<ConvenioQueryRepository>();
        services.AddSingleton<PacienteConvenioQueryRepository>();
        // Comandos convênio (scoped — EF)
        services.AddScoped<CriarConvenioCommandHandler>();
        services.AddScoped<AtualizarConvenioCommandHandler>();
        services.AddScoped<ExcluirConvenioCommandHandler>();
        services.AddScoped<AdicionarPlanoConvenioCommandHandler>();
        services.AddScoped<AtualizarPlanoConvenioCommandHandler>();
        services.AddScoped<InativarPlanoConvenioCommandHandler>();
        // Queries convênio (singleton — Dapper)
        services.AddSingleton<ListarConveniosQueryHandler>();
        services.AddSingleton<ObterConvenioQueryHandler>();
        // Comandos carteirinha (scoped — EF)
        services.AddScoped<CriarPacienteConvenioCommandHandler>();
        services.AddScoped<AtualizarPacienteConvenioCommandHandler>();
        services.AddScoped<ExcluirPacienteConvenioCommandHandler>();
        // Queries carteirinha (scoped: LGPD audit; singleton: check-in sem audit)
        services.AddScoped<ListarPacienteConveniosQueryHandler>();
        services.AddSingleton<ObterCarteirinhaAtivaCheckInQueryHandler>();
    }

    private static void RegistrarBuses(IServiceCollection services)
    {
        services.AddSingleton<ICommandBus>(sp =>
        {
            var bus = new MemoryCommandBus(sp, sp.GetRequiredService<IHttpContextAccessor>());
            bus.Register<FinalizarOnboardingCommand, FinalizarOnboardingCommandHandler>();
            bus.Register<CriarRegistroLocalUsuarioCommand, CriarRegistroLocalUsuarioCommandHandler>();
            bus.Register<AtualizarPerfilUsuarioCommand, AtualizarPerfilUsuarioCommandHandler>();
            bus.Register<CompletarOnboardingUsuarioCommand, CompletarOnboardingUsuarioCommandHandler>();
            bus.Register<RegistrarUltimoEstabelecimentoCommand, RegistrarUltimoEstabelecimentoCommandHandler>();
            bus.Register<CriarEstabelecimentoCommand, CriarEstabelecimentoCommandHandler>();
            bus.Register<AtualizarEstabelecimentoCommand, AtualizarEstabelecimentoCommandHandler>();
            bus.Register<AtualizarFuncionamentoCommand, AtualizarFuncionamentoCommandHandler>();
            bus.Register<AlterarFotoEstabelecimentoCommand, AlterarFotoEstabelecimentoCommandHandler>();
            bus.Register<RemoverFotoEstabelecimentoCommand, RemoverFotoEstabelecimentoCommandHandler>();
            bus.Register<AtualizarExigirDono2faCommand, AtualizarExigirDono2faCommandHandler>();
            bus.Register<CriarUnidadeCommand, CriarUnidadeCommandHandler>();
            bus.Register<AtualizarUnidadeCommand, AtualizarUnidadeCommandHandler>();
            bus.Register<DeletarUnidadeCommand, DeletarUnidadeCommandHandler>();
            bus.Register<CriarSalaCommand, CriarSalaCommandHandler>();
            bus.Register<AtualizarSalaCommand, AtualizarSalaCommandHandler>();
            bus.Register<DeletarSalaCommand, DeletarSalaCommandHandler>();
            bus.Register<DesativarSalaCommand, DesativarSalaCommandHandler>();
            bus.Register<ReativarSalaCommand, ReativarSalaCommandHandler>();
            bus.Register<CadastrarProfissionalCommand, CadastrarProfissionalCommandHandler>();
            bus.Register<AtualizarProfissionalCommand, AtualizarProfissionalCommandHandler>();
            bus.Register<AlterarFotoProfissionalCommand, AlterarFotoProfissionalCommandHandler>();
            bus.Register<RemoverFotoProfissionalCommand, RemoverFotoProfissionalCommandHandler>();
            bus.Register<ConvidarProfissionalCommand, ConvidarProfissionalCommandHandler>();
            bus.Register<AceitarConviteCommand, AceitarConviteCommandHandler>();
            bus.Register<AceitarConvitesPendentesDoUsuarioCommand, AceitarConvitesPendentesDoUsuarioCommandHandler>();
            bus.Register<InativarVinculoCommand, InativarVinculoCommandHandler>();
            bus.Register<ReativarVinculoCommand, ReativarVinculoCommandHandler>();
            bus.Register<ReenviarConviteCommand, ReenviarConviteCommandHandler>();
            bus.Register<AlterarModeloPermissaoDoVinculoCommand, AlterarModeloPermissaoDoVinculoCommandHandler>();
            bus.Register<AlterarEspecialidadeDoVinculoCommand, AlterarEspecialidadeDoVinculoCommandHandler>();
            bus.Register<AlterarProfissaoEspecialidadeDoVinculoCommand, AlterarProfissaoEspecialidadeDoVinculoCommandHandler>();
            // Item 4.2 — Solicitação inversa.
            bus.Register<SolicitarVinculoCommand, SolicitarVinculoCommandHandler>();
            bus.Register<AprovarSolicitacaoVinculoCommand, AprovarSolicitacaoVinculoCommandHandler>();
            bus.Register<RecusarSolicitacaoVinculoCommand, RecusarSolicitacaoVinculoCommandHandler>();
            bus.Register<CancelarSolicitacaoVinculoCommand, CancelarSolicitacaoVinculoCommandHandler>();
            bus.Register<CadastrarPacienteCommand, CadastrarPacienteCommandHandler>();
            bus.Register<AtualizarPacienteCommand, AtualizarPacienteCommandHandler>();
            bus.Register<DeletarPacienteCommand, DeletarPacienteCommandHandler>();
            bus.Register<CriarModeloDeProntuarioCommand, CriarModeloDeProntuarioCommandHandler>();
            bus.Register<AtualizarModeloDeProntuarioCommand, AtualizarModeloDeProntuarioCommandHandler>();
            bus.Register<ExcluirModeloDeProntuarioCommand, ExcluirModeloDeProntuarioCommandHandler>();
            bus.Register<AdicionarVariavelPoolCommand, AdicionarVariavelPoolCommandHandler>();
            bus.Register<AtualizarVariavelPoolCommand, AtualizarVariavelPoolCommandHandler>();
            bus.Register<ExcluirVariavelPoolCommand, ExcluirVariavelPoolCommandHandler>();
            bus.Register<IniciarProntuarioCommand, IniciarProntuarioCommandHandler>();
            bus.Register<RegistrarEvolucaoCommand, RegistrarEvolucaoCommandHandler>();
            bus.Register<RegistrarExportacaoProntuarioCommand, RegistrarExportacaoProntuarioCommandHandler>();
            bus.Register<RegistrarExportacaoEvolucaoCommand, RegistrarExportacaoEvolucaoCommandHandler>();
            bus.Register<AdicionarAnexoCommand, AdicionarAnexoCommandHandler>();
            bus.Register<CriarModeloPermissaoCommand, CriarModeloPermissaoCommandHandler>();
            bus.Register<AtualizarModeloPermissaoCommand, AtualizarModeloPermissaoCommandHandler>();
            bus.Register<ExcluirModeloPermissaoCommand, ExcluirModeloPermissaoCommandHandler>();
            bus.Register<CriarAgendamentoCommand, CriarAgendamentoCommandHandler>();
            bus.Register<AtualizarAgendamentoCommand, AtualizarAgendamentoCommandHandler>();
            bus.Register<CancelarAgendamentoCommand, CancelarAgendamentoCommandHandler>();
            bus.Register<ConfirmarAgendamentoCommand, ConfirmarAgendamentoCommandHandler>();
            bus.Register<ConcluirAgendamentoCommand, ConcluirAgendamentoCommandHandler>();
            bus.Register<RegistrarCheckInAgendamentoCommand, RegistrarCheckInAgendamentoCommandHandler>();
            bus.Register<AlocarSalaAgendamentoCommand, AlocarSalaAgendamentoCommandHandler>();
            // Fase 2 — link público de confirmação de presença.
            bus.Register<ConfirmarPresencaPublicaCommand, ConfirmarPresencaPublicaCommandHandler>();
            bus.Register<AdicionarListaEsperaCommand, AdicionarListaEsperaCommandHandler>();
            bus.Register<RemoverListaEsperaCommand, RemoverListaEsperaCommandHandler>();
            bus.Register<CriarItemInventarioCommand, CriarItemInventarioCommandHandler>();
            bus.Register<AtualizarItemInventarioCommand, AtualizarItemInventarioCommandHandler>();
            bus.Register<RegistrarMovimentacaoEstoqueCommand, RegistrarMovimentacaoEstoqueCommandHandler>();
            bus.Register<InativarItemInventarioCommand, InativarItemInventarioCommandHandler>();
            // Inventário — cadastros mestre (categoria/fabricante/fornecedor/local).
            bus.Register<CriarCategoriaEstoqueCommand,      CriarCategoriaEstoqueCommandHandler>();
            bus.Register<AtualizarCategoriaEstoqueCommand,  AtualizarCategoriaEstoqueCommandHandler>();
            bus.Register<InativarCategoriaEstoqueCommand,   InativarCategoriaEstoqueCommandHandler>();
            bus.Register<ReativarCategoriaEstoqueCommand,   ReativarCategoriaEstoqueCommandHandler>();
            bus.Register<CriarFabricanteEstoqueCommand,     CriarFabricanteEstoqueCommandHandler>();
            bus.Register<AtualizarFabricanteEstoqueCommand, AtualizarFabricanteEstoqueCommandHandler>();
            bus.Register<InativarFabricanteEstoqueCommand,  InativarFabricanteEstoqueCommandHandler>();
            bus.Register<ReativarFabricanteEstoqueCommand,  ReativarFabricanteEstoqueCommandHandler>();
            bus.Register<CriarFornecedorEstoqueCommand,     CriarFornecedorEstoqueCommandHandler>();
            bus.Register<AtualizarFornecedorEstoqueCommand, AtualizarFornecedorEstoqueCommandHandler>();
            bus.Register<InativarFornecedorEstoqueCommand,  InativarFornecedorEstoqueCommandHandler>();
            bus.Register<ReativarFornecedorEstoqueCommand,  ReativarFornecedorEstoqueCommandHandler>();
            bus.Register<CriarLocalEstoqueCommand,          CriarLocalEstoqueCommandHandler>();
            bus.Register<AtualizarLocalEstoqueCommand,      AtualizarLocalEstoqueCommandHandler>();
            bus.Register<InativarLocalEstoqueCommand,       InativarLocalEstoqueCommandHandler>();
            bus.Register<ReativarLocalEstoqueCommand,       ReativarLocalEstoqueCommandHandler>();
            bus.Register<CriarOrcamentoCommand, CriarOrcamentoCommandHandler>();
            bus.Register<AtualizarOrcamentoCommand, AtualizarOrcamentoCommandHandler>();
            bus.Register<EnviarOrcamentoCommand, EnviarOrcamentoCommandHandler>();
            bus.Register<AprovarOrcamentoCommand, AprovarOrcamentoCommandHandler>();
            bus.Register<RecusarOrcamentoCommand, RecusarOrcamentoCommandHandler>();
            bus.Register<CancelarOrcamentoCommand, CancelarOrcamentoCommandHandler>();
            bus.Register<ConverterOrcamentoEmCirurgiaCommand, ConverterOrcamentoEmCirurgiaCommandHandler>();
            // Fase 6.1 — Catálogos.
            bus.Register<CriarCatalogoCirurgiaCommand, CriarCatalogoCirurgiaCommandHandler>();
            bus.Register<AtualizarCatalogoCirurgiaCommand, AtualizarCatalogoCirurgiaCommandHandler>();
            bus.Register<RemoverCatalogoCirurgiaCommand, RemoverCatalogoCirurgiaCommandHandler>();
            bus.Register<CriarValorProfissionalCommand, CriarValorProfissionalCommandHandler>();
            bus.Register<AtualizarValorProfissionalCommand, AtualizarValorProfissionalCommandHandler>();
            bus.Register<RemoverValorProfissionalCommand, RemoverValorProfissionalCommandHandler>();
            bus.Register<SalvarConfiguracaoLocalCommand, SalvarConfiguracaoLocalCommandHandler>();
            bus.Register<CriarCatalogoEquipeCommand, CriarCatalogoEquipeCommandHandler>();
            bus.Register<AtualizarCatalogoEquipeCommand, AtualizarCatalogoEquipeCommandHandler>();
            bus.Register<RemoverCatalogoEquipeCommand, RemoverCatalogoEquipeCommandHandler>();
            bus.Register<CriarCatalogoImplanteCommand, CriarCatalogoImplanteCommandHandler>();
            bus.Register<AtualizarCatalogoImplanteCommand, AtualizarCatalogoImplanteCommandHandler>();
            bus.Register<RemoverCatalogoImplanteCommand, RemoverCatalogoImplanteCommandHandler>();
            bus.Register<CriarConfiguracaoPagamentoCommand, CriarConfiguracaoPagamentoCommandHandler>();
            bus.Register<AtualizarConfiguracaoPagamentoCommand, AtualizarConfiguracaoPagamentoCommandHandler>();
            bus.Register<RemoverConfiguracaoPagamentoCommand, RemoverConfiguracaoPagamentoCommandHandler>();
            bus.Register<CriarCatalogoProdutoCommand, CriarCatalogoProdutoCommandHandler>();
            bus.Register<AtualizarCatalogoProdutoCommand, AtualizarCatalogoProdutoCommandHandler>();
            bus.Register<RemoverCatalogoProdutoCommand, RemoverCatalogoProdutoCommandHandler>();
            bus.Register<VincularProdutoCirurgiaCommand, VincularProdutoCirurgiaCommandHandler>();
            bus.Register<AtualizarVinculoProdutoCirurgiaCommand, AtualizarVinculoProdutoCirurgiaCommandHandler>();
            bus.Register<DesvincularProdutoCirurgiaCommand, DesvincularProdutoCirurgiaCommandHandler>();
            bus.Register<CriarOrcamentoTeamRoleCommand, CriarOrcamentoTeamRoleCommandHandler>();
            bus.Register<AtualizarOrcamentoTeamRoleCommand, AtualizarOrcamentoTeamRoleCommandHandler>();
            bus.Register<RemoverOrcamentoTeamRoleCommand, RemoverOrcamentoTeamRoleCommandHandler>();
            bus.Register<CriarOrcamentoAnestesistaCommand, CriarOrcamentoAnestesistaCommandHandler>();
            bus.Register<AtualizarOrcamentoAnestesistaCommand, AtualizarOrcamentoAnestesistaCommandHandler>();
            bus.Register<RemoverOrcamentoAnestesistaCommand, RemoverOrcamentoAnestesistaCommandHandler>();
            bus.Register<CriarOrcamentoPacoteCommand, CriarOrcamentoPacoteCommandHandler>();
            bus.Register<AtualizarOrcamentoPacoteCommand, AtualizarOrcamentoPacoteCommandHandler>();
            bus.Register<RemoverOrcamentoPacoteCommand, RemoverOrcamentoPacoteCommandHandler>();
            // Item 3.3.A — procedimentos cirúrgicos.
            bus.Register<PlanejarProcedimentoCommand, PlanejarProcedimentoCommandHandler>();
            bus.Register<ConfirmarProcedimentoCommand, ConfirmarProcedimentoCommandHandler>();
            bus.Register<RegistrarRealizacaoCommand, RegistrarRealizacaoCommandHandler>();
            bus.Register<CancelarProcedimentoCommand, CancelarProcedimentoCommandHandler>();
            bus.Register<AtualizarEquipeCommand, AtualizarEquipeCommandHandler>();
            bus.Register<CriarLancamentoCommand, CriarLancamentoCommandHandler>();
            bus.Register<AtualizarLancamentoCommand, AtualizarLancamentoCommandHandler>();
            bus.Register<PagarLancamentoCommand, PagarLancamentoCommandHandler>();
            bus.Register<CancelarLancamentoCommand, CancelarLancamentoCommandHandler>();
            bus.Register<CriarCategoriaFinanceiraCommand, CriarCategoriaFinanceiraCommandHandler>();
            bus.Register<AtualizarCategoriaFinanceiraCommand, AtualizarCategoriaFinanceiraCommandHandler>();
            bus.Register<InativarCategoriaFinanceiraCommand, InativarCategoriaFinanceiraCommandHandler>();
            bus.Register<CriarFormaPagamentoCommand, CriarFormaPagamentoCommandHandler>();
            bus.Register<AtualizarFormaPagamentoCommand, AtualizarFormaPagamentoCommandHandler>();
            bus.Register<InativarFormaPagamentoCommand, InativarFormaPagamentoCommandHandler>();
            bus.Register<ExpirarOrcamentosVencidosCommand, ExpirarOrcamentosVencidosCommandHandler>();
            bus.Register<EnviarLembretesAgendamentosCommand, EnviarLembretesAgendamentosCommandHandler>();
            bus.Register<SalvarConfiguracaoAutomacaoCommand, SalvarConfiguracaoAutomacaoCommandHandler>();
            bus.Register<CriarRegraAutomacaoCommand, CriarRegraAutomacaoCommandHandler>();
            bus.Register<AtualizarRegraAutomacaoCommand, AtualizarRegraAutomacaoCommandHandler>();
            bus.Register<AtivarRegraAutomacaoCommand, AtivarRegraAutomacaoCommandHandler>();
            bus.Register<DesativarRegraAutomacaoCommand, DesativarRegraAutomacaoCommandHandler>();
            bus.Register<MarcarNotificacaoLidaCommand, MarcarNotificacaoLidaCommandHandler>();
            bus.Register<MarcarTodasNotificacoesLidasCommand, MarcarTodasNotificacoesLidasCommandHandler>();
            bus.Register<EmitirReceitaCommand, EmitirReceitaCommandHandler>();
            bus.Register<CancelarReceitaCommand, CancelarReceitaCommandHandler>();
            bus.Register<DuplicarReceitaCommand, DuplicarReceitaCommandHandler>();
            bus.Register<IniciarRascunhoReceitaCommand, IniciarRascunhoReceitaCommandHandler>();
            bus.Register<AtualizarRascunhoReceitaCommand, AtualizarRascunhoReceitaCommandHandler>();
            bus.Register<FinalizarReceitaCommand, FinalizarReceitaCommandHandler>();
            bus.Register<AtualizarConfiguracaoReceitaCommand, AtualizarConfiguracaoReceitaCommandHandler>();
            // Item 3.2 — Exame físico.
            bus.Register<RegistrarExameFisicoCommand, RegistrarExameFisicoCommandHandler>();
            bus.Register<AtualizarExameFisicoCommand, AtualizarExameFisicoCommandHandler>();
            // Atestados (2026-05-18).
            bus.Register<EmitirAtestadoCommand, EmitirAtestadoCommandHandler>();
            bus.Register<CriarModeloAtestadoCommand, CriarModeloAtestadoCommandHandler>();
            bus.Register<AtualizarModeloAtestadoCommand, AtualizarModeloAtestadoCommandHandler>();
            bus.Register<ExcluirModeloAtestadoCommand, ExcluirModeloAtestadoCommandHandler>();
            // Pedidos de exame (2026-05-18).
            bus.Register<EmitirPedidoExameCommand, EmitirPedidoExameCommandHandler>();
            // Termos de consentimento (Fase 1 — 2026-05-19).
            bus.Register<CriarModeloTermoCommand, CriarModeloTermoCommandHandler>();
            bus.Register<AtualizarModeloTermoCommand, AtualizarModeloTermoCommandHandler>();
            bus.Register<AlterarAtivoModeloTermoCommand, AlterarAtivoModeloTermoCommandHandler>();
            bus.Register<ExcluirModeloTermoCommand, ExcluirModeloTermoCommandHandler>();
            bus.Register<ClonarModeloTermoCommand, ClonarModeloTermoCommandHandler>();
            bus.Register<EmitirTermoCommand, EmitirTermoCommandHandler>();
            bus.Register<AnexarPdfTermoCommand, AnexarPdfTermoCommandHandler>();
            bus.Register<RevogarTermoCommand, RevogarTermoCommandHandler>();
            // Fase 4 — aceite público anônimo + reenvio de link autenticado.
            bus.Register<RegistrarRespostaPublicaTermoCommand, RegistrarRespostaPublicaTermoCommandHandler>();
            bus.Register<ReenviarLinkTermoCommand, ReenviarLinkTermoCommandHandler>();
            // Item 4.3 — LGPD.
            bus.Register<AnonimizarMinhaContaCommand, AnonimizarMinhaContaCommandHandler>();
            // Assinatura Digital ICP-Brasil (2026-06-01).
            bus.Register<VincularCertificadoCommand, VincularCertificadoCommandHandler>();
            bus.Register<RemoverCertificadoCommand, RemoverCertificadoCommandHandler>();
            bus.Register<DispararAssinaturaCommand, DispararAssinaturaCommandHandler>();
            bus.Register<ProcessarCallbackAssinaturaCommand, ProcessarCallbackAssinaturaCommandHandler>();
            bus.Register<ExpirarAssinaturasPendentesCommand, ExpirarAssinaturasPendentesCommandHandler>();
            // F3B — Pendências de atendimento (briefing 2026-06-10_012).
            bus.Register<ConcluirPendenciaManualCommand, ConcluirPendenciaManualCommandHandler>();
            // F4 — Marcar procedimento realizado (briefing 2026-06-10_013).
            bus.Register<MarcarProcedimentoRealizadoCommand, MarcarProcedimentoRealizadoCommandHandler>();
            // Cobranças F1/F2 (2026-06-10).
            bus.Register<RegistrarPagamentosCommand, RegistrarPagamentosCommandHandler>();
            bus.Register<EstornarPagamentoCommand, EstornarPagamentoCommandHandler>();
            bus.Register<SalvarTabelaPrecoConsultaCommand, SalvarTabelaPrecoConsultaCommandHandler>();
            bus.Register<InativarTabelaPrecoConsultaCommand, InativarTabelaPrecoConsultaCommandHandler>();
            bus.Register<SalvarConfigTaxaFormaPagamentoCommand, SalvarConfigTaxaFormaPagamentoCommandHandler>();
            // F6 — Convênios: estrutura base (2026-06-10_016)
            bus.Register<CriarConvenioCommand, CriarConvenioCommandHandler>();
            bus.Register<AtualizarConvenioCommand, AtualizarConvenioCommandHandler>();
            bus.Register<ExcluirConvenioCommand, ExcluirConvenioCommandHandler>();
            bus.Register<AdicionarPlanoConvenioCommand, AdicionarPlanoConvenioCommandHandler>();
            bus.Register<AtualizarPlanoConvenioCommand, AtualizarPlanoConvenioCommandHandler>();
            bus.Register<InativarPlanoConvenioCommand, InativarPlanoConvenioCommandHandler>();
            bus.Register<CriarPacienteConvenioCommand, CriarPacienteConvenioCommandHandler>();
            bus.Register<AtualizarPacienteConvenioCommand, AtualizarPacienteConvenioCommandHandler>();
            bus.Register<ExcluirPacienteConvenioCommand, ExcluirPacienteConvenioCommandHandler>();
            bus.Register<RegistrarGuiaCobrancaCommand, RegistrarGuiaCobrancaCommandHandler>();
            // F7 — Caixa diário + Comissões (2026-06-11_001)
            bus.Register<AbrirCaixaDiarioCommand, AbrirCaixaDiarioCommandHandler>();
            bus.Register<FecharCaixaDiarioCommand, FecharCaixaDiarioCommandHandler>();
            bus.Register<ReabrirCaixaDiarioCommand, ReabrirCaixaDiarioCommandHandler>();
            bus.Register<SalvarComissaoProfissionalCommand, SalvarComissaoProfissionalCommandHandler>();
            return bus;
        });

        services.AddSingleton<IRequestBus>(sp =>
        {
            var bus = new MemoryRequestBus(sp, sp.GetRequiredService<IHttpContextAccessor>());
            bus.Register<BootstrapMeQuery, BootstrapMeDto, BootstrapMeQueryHandlers>();
            bus.Register<VerificarCpfDisponivelQuery, VerificarCpfDisponivelResult, VerificarCpfDisponivelQueryHandler>();
            bus.Register<VerificarCnpjDisponivelQuery, VerificarCnpjDisponivelResult, VerificarCnpjDisponivelQueryHandler>();
            bus.Register<ListarMeusEstabelecimentosQuery, IEnumerable<EstabelecimentoDto>, ListarMeusEstabelecimentosQueryHandlers>();
            bus.Register<ListarUnidadesQuery, IEnumerable<UnidadeDto>, ListarUnidadesQueryHandlers>();
            bus.Register<ListarSalasQuery, IEnumerable<SalaDto>, ListarSalasQueryHandlers>();
            bus.Register<ListarTiposSalaQuery, IEnumerable<TipoSalaDto>, ListarTiposSalaQueryHandlers>();
            bus.Register<ObterProfissionalMeQuery, ProfissionalDto, ObterProfissionalMeQueryHandlers>();
            bus.Register<ListarProfissionaisEstabelecimentoQuery, IEnumerable<ProfissionalVinculadoDto>, ListarProfissionaisEstabelecimentoQueryHandlers>();
            bus.Register<ListarProfissionaisPublicoQuery, IEnumerable<ProfissionalPublicoDto>, ListarProfissionaisPublicoQueryHandlers>();
            bus.Register<ListarMeusConvitesQuery, IEnumerable<ConviteDto>, ListarMeusConvitesQueryHandlers>();
            // Item 4.2 — Solicitação inversa.
            bus.Register<ListarMinhasSolicitacoesVinculoQuery, IEnumerable<SolicitacaoVinculoDto>, ListarMinhasSolicitacoesVinculoQueryHandlers>();
            bus.Register<ListarSolicitacoesVinculoRecebidasQuery, IEnumerable<SolicitacaoVinculoDto>, ListarSolicitacoesVinculoRecebidasQueryHandlers>();
            bus.Register<ListarPacientesQuery, PaginaPacientesDto, ListarPacientesQueryHandlers>();
            bus.Register<BuscaRapidaPacientesQuery, IReadOnlyList<PacienteBuscaRapidaDto>, BuscaRapidaPacientesQueryHandler>();
            bus.Register<ObterPacienteStatsQuery, PacienteStatsDto, ObterPacienteStatsQueryHandler>();
            bus.Register<ObterPacienteQuery, PacienteDto, ObterPacienteQueryHandlers>();
            bus.Register<ExportarDadosPacienteQuery, PacienteExportLgpdDto, ExportarDadosPacienteQueryHandlers>();
            bus.Register<ListarDocumentosDoPacienteQuery, PaginaDocumentosDto, ListarDocumentosDoPacienteQueryHandlers>();
            bus.Register<ListarAcessosDoPacienteQuery, PaginaAcessosDto, ListarAcessosDoPacienteQueryHandlers>();
            bus.Register<ListarModelosDisponiveisQuery, IEnumerable<ModeloProntuarioDto>, ListarModelosDisponiveisQueryHandlers>();
            bus.Register<ObterModeloDeProntuarioQuery, ModeloProntuarioDto, ObterModeloDeProntuarioQueryHandlers>();
            bus.Register<ListarVariaveisPoolQuery, IEnumerable<VariavelPoolDto>, ListarVariaveisPoolQueryHandlers>();
            bus.Register<ObterProntuarioDoPacienteQuery, ProntuarioCompletoDto, ObterProntuarioDoPacienteQueryHandlers>();
            bus.Register<ListarEvolucoesProntuarioPacienteQuery, PaginaEvolucoesDto, ListarEvolucoesProntuarioPacienteQueryHandlers>();
            bus.Register<ContarEvolucoesProntuarioPacienteQuery, ContagemEvolucoesDto, ContarEvolucoesProntuarioPacienteQueryHandlers>();
            bus.Register<ListarAnexosDoProntuarioQuery, IEnumerable<AnexoDto>, ListarAnexosDoProntuarioQueryHandlers>();
            bus.Register<ObterUrlAnexoQuery, AnexoUrlDto, ObterUrlAnexoQueryHandlers>();
            bus.Register<ListarModelosPermissaoQuery, IEnumerable<ModeloPermissaoDto>, ListarModelosPermissaoQueryHandlers>();
            bus.Register<ListarAgendamentosQuery, PaginaAgendamentosDto, ListarAgendamentosQueryHandlers>();
            bus.Register<ContarAgendamentosPorDiaQuery, IEnumerable<ContagemPorDiaDto>, ContarAgendamentosPorDiaQueryHandler>();
            bus.Register<ObterAgendamentoQuery, AgendamentoDto, ObterAgendamentoQueryHandlers>();
            bus.Register<ConsultarDisponibilidadeQuery, DisponibilidadeSemanaDto, ConsultarDisponibilidadeQueryHandlers>();
            // Fase 2 — link público de confirmação de presença.
            bus.Register<ConsultarConfirmacaoPublicaQuery, ConfirmacaoPublicaDto, ConsultarConfirmacaoPublicaQueryHandler>();
            bus.Register<ListarListaEsperaQuery, PaginaListaEsperaDto, ListarListaEsperaQueryHandler>();
            bus.Register<ListarItensInventarioQuery, PaginaItensInventarioDto, ListarItensInventarioQueryHandlers>();
            bus.Register<ListarMovimentacoesQuery, PaginaMovimentacoesEstoqueDto, ListarMovimentacoesQueryHandlers>();
            bus.Register<ListarCategoriasEstoqueQuery,   PaginaCategoriasEstoqueDto,   ListarCategoriasEstoqueQueryHandlers>();
            bus.Register<ListarFabricantesEstoqueQuery,  PaginaFabricantesEstoqueDto,  ListarFabricantesEstoqueQueryHandlers>();
            bus.Register<ListarFornecedoresEstoqueQuery, PaginaFornecedoresEstoqueDto, ListarFornecedoresEstoqueQueryHandlers>();
            bus.Register<ListarLocaisEstoqueQuery,       PaginaLocaisEstoqueDto,       ListarLocaisEstoqueQueryHandlers>();
            bus.Register<ObterOpcoesCategoriasEstoqueQuery,   IReadOnlyList<OpcaoCadastroEstoqueDto>, ObterOpcoesCategoriasEstoqueQueryHandlers>();
            bus.Register<ObterOpcoesFabricantesEstoqueQuery,  IReadOnlyList<OpcaoCadastroEstoqueDto>, ObterOpcoesFabricantesEstoqueQueryHandlers>();
            bus.Register<ObterOpcoesFornecedoresEstoqueQuery, IReadOnlyList<OpcaoCadastroEstoqueDto>, ObterOpcoesFornecedoresEstoqueQueryHandlers>();
            bus.Register<ObterOpcoesLocaisEstoqueQuery,       IReadOnlyList<OpcaoCadastroEstoqueDto>, ObterOpcoesLocaisEstoqueQueryHandlers>();
            bus.Register<ListarOrcamentosQuery, IEnumerable<OrcamentoResumoDto>, ListarOrcamentosQueryHandlers>();
            bus.Register<ObterOrcamentoQuery, OrcamentoDto, ObterOrcamentoQueryHandlers>();
            bus.Register<PreviewOrcamentoQuery, PreviewOrcamentoDto, PreviewOrcamentoQueryHandler>();
            bus.Register<ObterOrcamentoPorAgendamentoQuery, OrcamentoResumoDto?, ObterOrcamentoPorAgendamentoQueryHandler>();
            bus.Register<ConsolidarProdutosOrcamentoQuery, List<ProdutoConsolidadoDto>, ConsolidarProdutosOrcamentoQueryHandler>();
            // Fase 6.1 — Queries de catálogos.
            bus.Register<ListarCatalogoCirurgiasQuery, IEnumerable<CatalogoCirurgiaDto>, ListarCatalogoCirurgiasQueryHandlers>();
            bus.Register<ListarValoresProfissionalQuery, IEnumerable<ValorProfissionalOrcamentoDto>, ListarValoresProfissionalQueryHandlers>();
            bus.Register<ListarConfiguracoesLocalQuery, IEnumerable<ConfiguracaoLocalCirurgiaDto>, ListarConfiguracoesLocalQueryHandlers>();
            bus.Register<ListarCatalogoEquipesQuery, IEnumerable<CatalogoEquipeEspecializadaDto>, ListarCatalogoEquipesQueryHandlers>();
            bus.Register<ListarCatalogoImplantesQuery, IEnumerable<CatalogoImplanteDto>, ListarCatalogoImplantesQueryHandlers>();
            bus.Register<ListarConfiguracoesPagamentoQuery, IEnumerable<ConfiguracaoPagamentoCatalogoDto>, ListarConfiguracoesPagamentoQueryHandlers>();
            bus.Register<ListarCatalogoProdutosQuery, IEnumerable<CatalogoProdutoDto>, ListarCatalogoProdutosQueryHandlers>();
            bus.Register<ListarProdutosDaCirurgiaQuery, IEnumerable<CatalogoCirurgiaProdutoDto>, ListarProdutosDaCirurgiaQueryHandlers>();
            bus.Register<ListarOrcamentoTeamRolesQuery, IEnumerable<OrcamentoTeamRoleDto>, ListarOrcamentoTeamRolesQueryHandlers>();
            bus.Register<ListarOrcamentoAnestesistasQuery, IEnumerable<OrcamentoAnestesistaListaDto>, ListarOrcamentoAnestesistasQueryHandlers>();
            bus.Register<ObterOrcamentoAnestesistaQuery, OrcamentoAnestesistaDto?, ObterOrcamentoAnestesistaQueryHandlers>();
            bus.Register<ListarOrcamentoPacotesQuery, IEnumerable<OrcamentoPacoteResumoDto>, ListarOrcamentoPacotesQueryHandlers>();
            bus.Register<ObterOrcamentoPacoteQuery, OrcamentoPacoteDetalheDto?, ObterOrcamentoPacoteQueryHandlers>();
            // Item 3.3.A — procedimentos cirúrgicos.
            bus.Register<ObterProcedimentoQuery, ProcedimentoCirurgicoDto, ObterProcedimentoQueryHandlers>();
            bus.Register<ListarProcedimentosDoPacienteQuery, IEnumerable<ProcedimentoCirurgicoResumoDto>, ObterProcedimentoQueryHandlers>();
            bus.Register<ListarProcedimentosPlanejadosQuery, IEnumerable<ProcedimentoCirurgicoResumoDto>, ListarProcedimentosPlanejadosQueryHandlers>();
            bus.Register<ListarLancamentosQuery, PaginaLancamentosDto, ListarLancamentosQueryHandlers>();
            bus.Register<ObterResumoFinanceiroQuery, ResumoFinanceiroDto, ObterResumoFinanceiroQueryHandlers>();
            bus.Register<ListarCategoriasFinanceirasQuery, IEnumerable<CategoriaFinanceiraDto>, ListarCategoriasFinanceirasQueryHandlers>();
            bus.Register<ListarFormasPagamentoQuery, IEnumerable<FormaPagamentoDto>, ListarFormasPagamentoQueryHandlers>();
            bus.Register<DashboardQuery, DashboardDto, DashboardQueryHandlers>();
            bus.Register<RelatorioFaturamentoQuery, IEnumerable<FaturamentoCategoriaDto>, RelatorioFaturamentoQueryHandlers>();
            bus.Register<RelatorioAgendamentosQuery, RelatorioAgendamentosDto, RelatorioAgendamentosQueryHandlers>();
            // Item 4.1 — relatórios consolidados.
            bus.Register<RelatorioFinanceiroQuery, RelatorioFinanceiroDto, RelatorioFinanceiroQueryHandler>();
            bus.Register<RelatorioOperacionalQuery, RelatorioOperacionalDto, RelatorioOperacionalQueryHandler>();
            bus.Register<RelatorioPessoasQuery, RelatorioPessoasDto, RelatorioPessoasQueryHandler>();
            bus.Register<RelatorioOrcamentosQuery, RelatorioOrcamentosDto, RelatorioOrcamentosQueryHandler>();
            // Item 4.3 — LGPD.
            bus.Register<ExportarMeusDadosQuery, MeusDadosLgpdDto, ExportarMeusDadosLgpdQueryHandlers>();
            bus.Register<ObterConfiguracaoAutomacaoQuery, ConfiguracaoAutomacaoDto, ObterConfiguracaoAutomacaoQueryHandlers>();
            bus.Register<ListarProfissoesQuery, IEnumerable<ProfissaoListadaDto>, ListarProfissoesQueryHandlers>();
            bus.Register<ListarEspecialidadesQuery, IEnumerable<EspecialidadeListadaDto>, ListarEspecialidadesQueryHandlers>();
            bus.Register<ListarRegioesCatalogoQuery, IEnumerable<RegiaoCatalogoDto>, ListarRegioesCatalogoQueryHandlers>();
            // Item 4.13 — Catálogo TUSS/CBHPM.
            bus.Register<BuscarProcedimentoCatalogoQuery, IEnumerable<ProcedimentoCatalogoDto>, BuscarProcedimentoCatalogoQueryHandlers>();
            bus.Register<ObterProcedimentoPorCodigoQuery, ProcedimentoCatalogoDto?, ObterProcedimentoPorCodigoQueryHandlers>();
            bus.Register<ListarRegrasAutomacaoQuery, IEnumerable<RegraAutomacaoDto>, ListarRegrasAutomacaoQueryHandlers>();
            bus.Register<ListarEventosAutomacaoQuery, IEnumerable<EventoAutomacaoDto>, ListarEventosAutomacaoQueryHandlers>();
            bus.Register<ListarNotificacoesQuery, PaginaNotificacoesDto, ListarNotificacoesQueryHandlers>();
            bus.Register<ContadorNaoLidasQuery, ContadorNaoLidasDto, ContadorNaoLidasQueryHandlers>();
            // Item 2.7 — Assinatura/Planos.
            bus.Register<ListarPlanosQuery, IEnumerable<PlanoDto>, ListarPlanosQueryHandlers>();
            bus.Register<ObterMinhaAssinaturaQuery, AssinaturaDto?, ObterMinhaAssinaturaQueryHandlers>();
            // Item 3.1 — Receitas.
            bus.Register<ListarReceitasDoPacienteQuery, PaginaReceitasDto, ListarReceitasDoPacienteQueryHandlers>();
            bus.Register<ObterReceitaQuery, ReceitaDto, ObterReceitaQueryHandlers>();
            bus.Register<ObterConfiguracaoReceitaQuery, ConfiguracaoReceitaDto, ObterConfiguracaoReceitaQueryHandlers>();
            // Item 3.2 — Exame físico (uma classe ObterExameFisicoQueryHandlers implementa as 4 queries; auditam acesso → scoped).
            bus.Register<ObterExameFisicoQuery, ExameFisicoDto?, ObterExameFisicoQueryHandlers>();
            bus.Register<ObterExameFisicoPorEvolucaoQuery, ExameFisicoDto?, ObterExameFisicoQueryHandlers>();
            bus.Register<ListarExamesFisicosDoPacienteQuery, PaginaExamesFisicosDto, ObterExameFisicoQueryHandlers>();
            bus.Register<TimelineExamesFisicosQuery, IEnumerable<ExameFisicoResumoDto>, ObterExameFisicoQueryHandlers>();
            // Atestados (2026-05-18).
            bus.Register<ListarAtestadosDoPacienteQuery, PaginaAtestadosDto, ListarAtestadosDoPacienteQueryHandlers>();
            bus.Register<ObterAtestadoQuery, AtestadoDto, ObterAtestadoQueryHandlers>();
            bus.Register<ListarModelosAtestadoQuery, IReadOnlyList<ModeloAtestadoDto>, ListarModelosAtestadoQueryHandlers>();
            // Pedidos de exame (2026-05-18).
            bus.Register<ListarPedidosExameDoPacienteQuery, PaginaPedidosExameDto, ListarPedidosExameDoPacienteQueryHandlers>();
            bus.Register<ObterPedidoExameQuery, PedidoExameDto, ObterPedidoExameQueryHandlers>();
            // Termos de consentimento (Fase 1 — 2026-05-19).
            bus.Register<ListarModelosTermoQuery, PaginaModelosTermoDto, ListarModelosTermoQueryHandlers>();
            bus.Register<ListarModelosPadraoTermoQuery, IReadOnlyList<TermoModeloDto>, ListarModelosPadraoTermoQueryHandlers>();
            bus.Register<ObterModeloTermoQuery, TermoModeloDto, ObterModeloTermoQueryHandlers>();
            bus.Register<ListarVariaveisDisponiveisQuery, IReadOnlyList<VariavelDisponivelDto>, ListarVariaveisDisponiveisQueryHandlers>();
            bus.Register<ListarTermosDoPacienteQuery, IReadOnlyList<TermoEmitidoResumoDto>, ListarTermosDoPacienteQueryHandlers>();
            bus.Register<ObterTermoEmitidoQuery, TermoEmitidoDetalheDto, ObterTermoEmitidoQueryHandlers>();
            bus.Register<ObterUrlPdfTermoQuery, TermoPdfUrlDto, ObterUrlPdfTermoQueryHandlers>();
            // Fase 4 — query anônima via token público.
            bus.Register<ObterTermoPublicoPorTokenQuery, TermoPublicoDto, ObterTermoPublicoPorTokenQueryHandler>();
            // Assinatura Digital ICP-Brasil (2026-06-01).
            bus.Register<ObterStatusAssinaturaQuery, StatusAssinaturaDto, ObterStatusAssinaturaQueryHandler>();
            bus.Register<ObterCertificadoVinculadoQuery, CertificadoVinculadoDto?, ObterCertificadoVinculadoQueryHandler>();
            // F3B — Pendências de atendimento (briefing 2026-06-10_012).
            bus.Register<ListarPendenciasAbertasQuery, IReadOnlyList<PendenciaAbertaDto>, ListarPendenciasAbertasQueryHandler>();
            // F4 — Preview modal MarcarProcedimentoRealizado (briefing 2026-06-10_013).
            bus.Register<PreviewProcedimentoRealizadoQuery, PreviewProcedimentoRealizadoDto, PreviewProcedimentoRealizadoQueryHandler>();
            // F5 — Pré-preenchimento form de orçamento (briefing 2026-06-10_014).
            bus.Register<ObterProcedimentosIndicadosQuery, IEnumerable<ProcedimentoIndicadoDto>, ObterProcedimentosIndicadosQueryHandler>();
            // Cobranças F1/F2 (2026-06-10).
            bus.Register<ObterCobrancaDaAgendaQuery, CobrancaDetalheDto?, ObterCobrancaDaAgendaQueryHandlers>();
            bus.Register<ObterValorSugeridoCheckInQuery, ValorSugeridoCheckInDto, ObterValorSugeridoCheckInQueryHandlers>();
            bus.Register<ListarTabelaPrecoConsultaQuery, IEnumerable<TabelaPrecoConsultaDto>, ListarTabelaPrecoConsultaQueryHandlers>();
            bus.Register<ListarConfigTaxaFormaPagamentoQuery, IEnumerable<ConfigTaxaFormaPagamentoDto>, ListarConfigTaxaFormaPagamentoQueryHandlers>();
            bus.Register<ObterFinanceiroAbaQuery, FinanceiroAbaDto, ObterFinanceiroAbaQueryHandler>();
            // F8: recibo de pagamento
            bus.Register<EmitirReciboPagamentoQuery, byte[], EmitirReciboPagamentoQueryHandler>();
            // F6 — Convênios: estrutura base (2026-06-10_016)
            bus.Register<ListarConveniosQuery, IReadOnlyList<ConvenioListadoDto>, ListarConveniosQueryHandler>();
            bus.Register<ObterConvenioQuery, ConvenioDetalheDto?, ObterConvenioQueryHandler>();
            bus.Register<ListarPacienteConveniosQuery, IReadOnlyList<PacienteConvenioDto>, ListarPacienteConveniosQueryHandler>();
            bus.Register<ObterCarteirinhaAtivaCheckInQuery, IReadOnlyList<CarteirinhaCheckInDto>, ObterCarteirinhaAtivaCheckInQueryHandler>();
            // F7 — Extrato/KPIs/Caixa/Comissões (2026-06-11_001)
            bus.Register<ObterKpisFinanceiroQuery, KpisFinanceiroDto, ObterKpisFinanceiroQueryHandler>();
            bus.Register<ListarExtratoQuery, PaginaLancamentosExtratoDto, ListarExtratoQueryHandler>();
            bus.Register<ObterCaixaDiarioQuery, CaixaDiarioDto?, ObterCaixaDiarioQueryHandler>();
            bus.Register<ObterComissoesPeriodoQuery, ComissaoPeriodoDto, ObterComissoesPeriodoQueryHandler>();
            bus.Register<ObterConfigComissaoQuery, ConfigComissaoDto, ObterConfigComissaoQueryHandler>();
            // F7 redesign — Export de extrato (briefing 2026-06-11_002)
            bus.Register<ExportarExtratoQuery, ExportarExtratoResultDto, ExportarExtratoQueryHandler>();
            return bus;
        });

        services.AddSingleton<IEventBus>(sp =>
        {
            var bus = new MemoryEventBus(sp, sp.GetRequiredService<IHttpContextAccessor>());
            bus.Register<UsuarioCriadoEvent, UsuarioCriadoEventHandler>();
            bus.Register<EstabelecimentoCriadoEvent, EstabelecimentoCriadoEventHandler>();
            bus.Register<EstabelecimentoCriadoEvent, CriarModeloPadraoAoCriarEstabelecimentoHandler>();
            bus.Register<EstabelecimentoCriadoEvent, CriarUnidadePadraoAoCriarEstabelecimentoHandler>();
            bus.Register<EstabelecimentoCriadoEvent, CriarSeedFinanceiroAoCriarEstabelecimentoHandler>();
            // Item 2.7 — inicia trial automático de 14 dias ao criar estabelecimento.
            bus.Register<EstabelecimentoCriadoEvent, IniciarTrialAoCriarEstabelecimentoHandler>();
            bus.Register<ProfissionalCadastradoEvent, ProfissionalCadastradoEventHandler>();
            bus.Register<ProfissionalConvidadoEvent, ProfissionalConvidadoEventHandler>();
            bus.Register<VinculoAceitoEvent, VinculoAceitoEventHandler>();
            // Item 4.2 — Solicitação inversa: 1 handler que cria o vínculo + 2 de notificação.
            bus.Register<SolicitacaoVinculoAprovadaEvent, AoAprovarSolicitacaoCriarVinculoHandler>();
            bus.Register<SolicitacaoVinculoCriadaEvent, NotificarSolicitacaoCriadaHandler>();
            bus.Register<SolicitacaoVinculoAprovadaEvent, NotificarSolicitacaoRespondidaHandler>();
            bus.Register<SolicitacaoVinculoRecusadaEvent, NotificarSolicitacaoRespondidaHandler>();
            bus.Register<PacienteCadastradoEvent, PacienteCadastradoEventHandler>();
            bus.Register<ProntuarioIniciadoEvent, ProntuarioIniciadoEventHandler>();
            bus.Register<EvolucaoRegistradaEvent, EvolucaoRegistradaEventHandler>();
            bus.Register<AgendamentoCriadoEvent, AgendamentoCriadoEventHandler>();
            bus.Register<AgendamentoCanceladoEvent, AgendamentoCanceladoEventHandler>();
            bus.Register<AgendamentoReagendadoEvent, EnviarEmailAgendamentoReagendadoEventHandler>();
            bus.Register<EstoqueAbaixoMinimoEvent, EstoqueAbaixoMinimoEventHandler>();
            bus.Register<OrcamentoCriadoEvent, OrcamentoCriadoEventHandler>();
            // F3B — Conclusão automática de pendências (R7-R11/CA63-CA65).
            // Ouve eventos EXISTENTES — nunca derruba o evento principal (fan-out independente).
            bus.Register<ReceitaEmitidaEvent, ConcluirPendenciaAoEmitirReceitaHandler>();
            bus.Register<AtestadoEmitidoEvent, ConcluirPendenciaAoEmitirAtestadoHandler>();
            bus.Register<PedidoExameEmitidoEvent, ConcluirPendenciaAoEmitirPedidoExameHandler>();
            bus.Register<OrcamentoCriadoEvent, ConcluirPendenciaAoCriarOrcamentoHandler>();
            bus.Register<AgendamentoCriadoEvent, ConcluirPendenciaAoCriarAgendamentoHandler>();
            bus.Register<OrcamentoAprovadoEvent, OrcamentoAprovadoEventHandler>();
            // Item 3.3.A — confirmação de procedimento → notificação à equipe operacional.
            bus.Register<ProcedimentoConfirmadoEvent, NotificarEquipeAoConfirmarHandler>();
            bus.Register<LancamentoCriadoEvent, LancamentoCriadoEventHandler>();
            bus.Register<LancamentoPagoEvent, LancamentoPagoEventHandler>();
            // Item 2.3: convite de profissional → notificação in-app.
            bus.Register<ProfissionalConvidadoEvent, NotificarConviteAoConvidarProfissionalHandler>();
            // Item 2.4: notificação criada → push em tempo real para o usuário via SignalR.
            bus.Register<NotificacaoCriadaEvent, NotificacaoCriadaSignalRBridge>();
            // Modelo de permissão do vínculo trocado → cliente revalida permissões.
            bus.Register<VinculoModeloPermissaoAlteradoEvent, PermissoesAlteradasSignalRBridge>();
            // Item 2.2: gatilhos de regras de automação. NÃO escutar NotificacaoCriadaEvent
            // — caso contrário regras que enviam notificação criariam loop infinito.
            bus.Register<AgendamentoCriadoEvent, EnfileirarAutomacaoAgendamentoCriadoHandler>();
            bus.Register<OrcamentoAprovadoEvent, EnfileirarAutomacaoOrcamentoAprovadoHandler>();
            bus.Register<LancamentoCriadoEvent, EnfileirarAutomacaoLancamentoCriadoHandler>();
            // Fase 4 — Termos de consentimento (aceite público).
            // Emissão com canal=email dispara envio ao paciente.
            bus.Register<TermoEmitidoEvent, Imedto.Backend.Application.Termos.Events.EnviarEmailTermoLinkEventHandler>();
            // Paciente assina/recusa → notificar emissor.
            bus.Register<TermoAssinadoEvent, Imedto.Backend.Application.Termos.Events.NotificarEmissorTermoAssinadoEventHandler>();
            bus.Register<TermoRecusadoEvent, Imedto.Backend.Application.Termos.Events.NotificarEmissorTermoRecusadoEventHandler>();
            return bus;
        });
    }
}
