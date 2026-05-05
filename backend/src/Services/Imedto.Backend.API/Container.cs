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
using Imedto.Backend.Application.Receitas.Commands;
using Imedto.Backend.Application.Receitas.Queries;
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
/// 4. Gerar migration EF + copiar SQL idempotente para supabase/migrations/.
/// </summary>
public static class Container
{
    public static IServiceCollection Install(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache(); // usado pelo AssinaturaService (gating de feature).
        services.AddInfrastructure(configuration);
        RegistrarIa(services, configuration);
        RegistrarHandlers(services);
        RegistrarBuses(services);

        // Admin
        services.AddScoped<Domain.Admin.IAdminResetService, AdminResetService>();

        // Idempotência
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IdempotencyFilter>();

        // Scheduler de jobs (item 2.1) — BackgroundService nativo + advisory lock para single-leader.
        // Repositório é Scoped (usa AppDbContext); handlers IJobHandler também Scoped — o scheduler
        // resolve via IServiceScopeFactory para obter um DbContext fresco por execução de job.
        services.AddScoped<IJobAgendadoRepository, JobAgendadoRepository>();
        services.AddScoped<IJobHandler, LimparAuditAntigoJob>();
        services.AddScoped<IJobHandler, ExpirarTrialsJob>(); // item 2.7
        services.AddScoped<IJobHandler, LimparCacheIaJob>(); // item 3.8
        services.AddSingleton<JobScheduler>();
        services.AddHostedService(sp => sp.GetRequiredService<JobScheduler>());

        // Item 2.7 — Seed do catálogo de planos (roda uma vez na startup, idempotente).
        services.AddHostedService<SeedPlanosHostedService>();

        return services;
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
        services.AddSingleton<BootstrapMeQueryHandlers>();

        // Onboarding
        services.AddScoped<FinalizarOnboardingCommandHandler>();

        // Usuarios
        services.AddSingleton<UsuarioQueryRepository>();
        services.AddScoped<CriarRegistroLocalUsuarioCommandHandler>();
        services.AddScoped<AtualizarPerfilUsuarioCommandHandler>();
        services.AddScoped<CompletarOnboardingUsuarioCommandHandler>();
        services.AddSingleton<VerificarCpfDisponivelQueryHandler>();
        services.AddScoped<UsuarioCriadoEventHandler>();

        // Estabelecimentos
        services.AddScoped<CriarEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarFuncionamentoCommandHandler>();
        services.AddScoped<AlterarFotoEstabelecimentoCommandHandler>();
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
        services.AddSingleton<ListarSalasQueryHandlers>();
        services.AddSingleton<ListarTiposSalaQueryHandlers>();
        services.AddSingleton<SalaQueryRepository>();

        // Profissionais
        services.AddScoped<CadastrarProfissionalCommandHandler>();
        services.AddScoped<AtualizarProfissionalCommandHandler>();
        services.AddScoped<AlterarFotoProfissionalCommandHandler>();
        services.AddSingleton<ObterProfissionalMeQueryHandlers>();
        services.AddScoped<ProfissionalCadastradoEventHandler>();
        services.AddSingleton<ProfissionalQueryRepository>();

        // Vínculos
        services.AddScoped<ConvidarProfissionalCommandHandler>();
        services.AddScoped<AceitarConviteCommandHandler>();
        services.AddScoped<InativarVinculoCommandHandler>();
        services.AddScoped<AlterarModeloPermissaoDoVinculoCommandHandler>();
        services.AddScoped<ListarProfissionaisEstabelecimentoQueryHandlers>();
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
        // Scoped: Obter/Export auditam acesso via IPacienteAcessoLogService (LGPD).
        services.AddScoped<ObterPacienteQueryHandlers>();
        services.AddScoped<ExportarDadosPacienteQueryHandlers>();
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
        services.AddScoped<IniciarProntuarioCommandHandler>();
        services.AddScoped<RegistrarEvolucaoCommandHandler>();
        services.AddScoped<ObterProntuarioDoPacienteQueryHandlers>(); // scoped — injeta IProntuarioAcessoLogService (scoped)
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
        // Item 4.7 — provedor real (Resend) com fallback para NoOp se Email:ApiKey vazio.
        // Decisão tomada na resolução (factory) para ler IConfiguration do scope sem
        // precisar propagar a configuration por todos os métodos de Registrar*.
        services.AddScoped<IEmailService>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var apiKey = cfg["Email:ApiKey"] ?? cfg["Email:ResendApiKey"];
            return string.IsNullOrWhiteSpace(apiKey)
                ? ActivatorUtilities.CreateInstance<NoOpEmailService>(sp)
                : ActivatorUtilities.CreateInstance<ResendEmailService>(sp);
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

        // Item 4.3 — LGPD: anonimização, consentimentos e exportação.
        services.AddScoped<ILgpdAnonimizacaoRepository, LgpdAnonimizacaoRepository>();
        services.AddScoped<ILgpdConsentimentoRepository, LgpdConsentimentoRepository>();
        services.AddScoped<IAnonimizacaoService, AnonimizacaoService>();
        services.AddSingleton<LgpdQueryRepository>();
        services.AddScoped<RegistrarConsentimentoCommandHandler>();
        services.AddScoped<AnonimizarMinhaContaCommandHandler>();
        services.AddSingleton<ExportarMeusDadosLgpdQueryHandlers>();
        services.AddScoped<ListarMeusConsentimentosQueryHandlers>();
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
            bus.Register<CriarEstabelecimentoCommand, CriarEstabelecimentoCommandHandler>();
            bus.Register<AtualizarEstabelecimentoCommand, AtualizarEstabelecimentoCommandHandler>();
            bus.Register<AtualizarFuncionamentoCommand, AtualizarFuncionamentoCommandHandler>();
            bus.Register<AlterarFotoEstabelecimentoCommand, AlterarFotoEstabelecimentoCommandHandler>();
            bus.Register<CriarUnidadeCommand, CriarUnidadeCommandHandler>();
            bus.Register<AtualizarUnidadeCommand, AtualizarUnidadeCommandHandler>();
            bus.Register<DeletarUnidadeCommand, DeletarUnidadeCommandHandler>();
            bus.Register<CriarSalaCommand, CriarSalaCommandHandler>();
            bus.Register<AtualizarSalaCommand, AtualizarSalaCommandHandler>();
            bus.Register<DeletarSalaCommand, DeletarSalaCommandHandler>();
            bus.Register<CadastrarProfissionalCommand, CadastrarProfissionalCommandHandler>();
            bus.Register<AtualizarProfissionalCommand, AtualizarProfissionalCommandHandler>();
            bus.Register<AlterarFotoProfissionalCommand, AlterarFotoProfissionalCommandHandler>();
            bus.Register<ConvidarProfissionalCommand, ConvidarProfissionalCommandHandler>();
            bus.Register<AceitarConviteCommand, AceitarConviteCommandHandler>();
            bus.Register<InativarVinculoCommand, InativarVinculoCommandHandler>();
            bus.Register<AlterarModeloPermissaoDoVinculoCommand, AlterarModeloPermissaoDoVinculoCommandHandler>();
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
            bus.Register<AdicionarAnexoCommand, AdicionarAnexoCommandHandler>();
            bus.Register<CriarModeloPermissaoCommand, CriarModeloPermissaoCommandHandler>();
            bus.Register<AtualizarModeloPermissaoCommand, AtualizarModeloPermissaoCommandHandler>();
            bus.Register<ExcluirModeloPermissaoCommand, ExcluirModeloPermissaoCommandHandler>();
            bus.Register<CriarAgendamentoCommand, CriarAgendamentoCommandHandler>();
            bus.Register<AtualizarAgendamentoCommand, AtualizarAgendamentoCommandHandler>();
            bus.Register<CancelarAgendamentoCommand, CancelarAgendamentoCommandHandler>();
            bus.Register<ConfirmarAgendamentoCommand, ConfirmarAgendamentoCommandHandler>();
            bus.Register<ConcluirAgendamentoCommand, ConcluirAgendamentoCommandHandler>();
            bus.Register<AdicionarListaEsperaCommand, AdicionarListaEsperaCommandHandler>();
            bus.Register<RemoverListaEsperaCommand, RemoverListaEsperaCommandHandler>();
            bus.Register<CriarItemInventarioCommand, CriarItemInventarioCommandHandler>();
            bus.Register<AtualizarItemInventarioCommand, AtualizarItemInventarioCommandHandler>();
            bus.Register<RegistrarMovimentacaoEstoqueCommand, RegistrarMovimentacaoEstoqueCommandHandler>();
            bus.Register<InativarItemInventarioCommand, InativarItemInventarioCommandHandler>();
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
            // Item 4.3 — LGPD.
            bus.Register<RegistrarConsentimentoCommand, RegistrarConsentimentoCommandHandler>();
            bus.Register<AnonimizarMinhaContaCommand, AnonimizarMinhaContaCommandHandler>();
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
            bus.Register<ListarMeusConvitesQuery, IEnumerable<ConviteDto>, ListarMeusConvitesQueryHandlers>();
            // Item 4.2 — Solicitação inversa.
            bus.Register<ListarMinhasSolicitacoesVinculoQuery, IEnumerable<SolicitacaoVinculoDto>, ListarMinhasSolicitacoesVinculoQueryHandlers>();
            bus.Register<ListarSolicitacoesVinculoRecebidasQuery, IEnumerable<SolicitacaoVinculoDto>, ListarSolicitacoesVinculoRecebidasQueryHandlers>();
            bus.Register<ListarPacientesQuery, PaginaPacientesDto, ListarPacientesQueryHandlers>();
            bus.Register<ObterPacienteQuery, PacienteDto, ObterPacienteQueryHandlers>();
            bus.Register<ExportarDadosPacienteQuery, PacienteExportLgpdDto, ExportarDadosPacienteQueryHandlers>();
            bus.Register<ListarModelosDisponiveisQuery, IEnumerable<ModeloProntuarioDto>, ListarModelosDisponiveisQueryHandlers>();
            bus.Register<ObterModeloDeProntuarioQuery, ModeloProntuarioDto, ObterModeloDeProntuarioQueryHandlers>();
            bus.Register<ListarVariaveisPoolQuery, IEnumerable<VariavelPoolDto>, ListarVariaveisPoolQueryHandlers>();
            bus.Register<ObterProntuarioDoPacienteQuery, ProntuarioCompletoDto, ObterProntuarioDoPacienteQueryHandlers>();
            bus.Register<ContarEvolucoesProntuarioPacienteQuery, ContagemEvolucoesDto, ContarEvolucoesProntuarioPacienteQueryHandlers>();
            bus.Register<ListarAnexosDoProntuarioQuery, IEnumerable<AnexoDto>, ListarAnexosDoProntuarioQueryHandlers>();
            bus.Register<ObterUrlAnexoQuery, AnexoUrlDto, ObterUrlAnexoQueryHandlers>();
            bus.Register<ListarModelosPermissaoQuery, IEnumerable<ModeloPermissaoDto>, ListarModelosPermissaoQueryHandlers>();
            bus.Register<ListarAgendamentosQuery, PaginaAgendamentosDto, ListarAgendamentosQueryHandlers>();
            bus.Register<ContarAgendamentosPorDiaQuery, IEnumerable<ContagemPorDiaDto>, ContarAgendamentosPorDiaQueryHandler>();
            bus.Register<ObterAgendamentoQuery, AgendamentoDto, ObterAgendamentoQueryHandlers>();
            bus.Register<ConsultarDisponibilidadeQuery, DisponibilidadeSemanaDto, ConsultarDisponibilidadeQueryHandlers>();
            bus.Register<ListarListaEsperaQuery, PaginaListaEsperaDto, ListarListaEsperaQueryHandler>();
            bus.Register<ListarItensInventarioQuery, PaginaItensInventarioDto, ListarItensInventarioQueryHandlers>();
            bus.Register<ListarMovimentacoesQuery, PaginaMovimentacoesEstoqueDto, ListarMovimentacoesQueryHandlers>();
            bus.Register<ListarOrcamentosQuery, IEnumerable<OrcamentoResumoDto>, ListarOrcamentosQueryHandlers>();
            bus.Register<ObterOrcamentoQuery, OrcamentoDto, ObterOrcamentoQueryHandlers>();
            bus.Register<PreviewOrcamentoQuery, PreviewOrcamentoDto, PreviewOrcamentoQueryHandler>();
            // Fase 6.1 — Queries de catálogos.
            bus.Register<ListarCatalogoCirurgiasQuery, IEnumerable<CatalogoCirurgiaDto>, ListarCatalogoCirurgiasQueryHandlers>();
            bus.Register<ListarValoresProfissionalQuery, IEnumerable<ValorProfissionalOrcamentoDto>, ListarValoresProfissionalQueryHandlers>();
            bus.Register<ListarConfiguracoesLocalQuery, IEnumerable<ConfiguracaoLocalCirurgiaDto>, ListarConfiguracoesLocalQueryHandlers>();
            bus.Register<ListarCatalogoEquipesQuery, IEnumerable<CatalogoEquipeEspecializadaDto>, ListarCatalogoEquipesQueryHandlers>();
            bus.Register<ListarCatalogoImplantesQuery, IEnumerable<CatalogoImplanteDto>, ListarCatalogoImplantesQueryHandlers>();
            bus.Register<ListarConfiguracoesPagamentoQuery, IEnumerable<ConfiguracaoPagamentoCatalogoDto>, ListarConfiguracoesPagamentoQueryHandlers>();
            bus.Register<ListarCatalogoProdutosQuery, IEnumerable<CatalogoProdutoDto>, ListarCatalogoProdutosQueryHandlers>();
            bus.Register<ListarProdutosDaCirurgiaQuery, IEnumerable<CatalogoCirurgiaProdutoDto>, ListarProdutosDaCirurgiaQueryHandlers>();
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
            bus.Register<ListarMeusConsentimentosQuery, IEnumerable<ConsentimentoDto>, ListarMeusConsentimentosQueryHandlers>();
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
            bus.Register<EstoqueAbaixoMinimoEvent, EstoqueAbaixoMinimoEventHandler>();
            bus.Register<OrcamentoCriadoEvent, OrcamentoCriadoEventHandler>();
            bus.Register<OrcamentoAprovadoEvent, OrcamentoAprovadoEventHandler>();
            // Item 3.3.A — confirmação de procedimento → notificação à equipe operacional.
            bus.Register<ProcedimentoConfirmadoEvent, NotificarEquipeAoConfirmarHandler>();
            bus.Register<LancamentoCriadoEvent, LancamentoCriadoEventHandler>();
            bus.Register<LancamentoPagoEvent, LancamentoPagoEventHandler>();
            // Item 2.3: convite de profissional → notificação in-app.
            bus.Register<ProfissionalConvidadoEvent, NotificarConviteAoConvidarProfissionalHandler>();
            // Item 2.4: notificação criada → push em tempo real para o usuário via SignalR.
            bus.Register<NotificacaoCriadaEvent, NotificacaoCriadaSignalRBridge>();
            // Item 2.2: gatilhos de regras de automação. NÃO escutar NotificacaoCriadaEvent
            // — caso contrário regras que enviam notificação criariam loop infinito.
            bus.Register<AgendamentoCriadoEvent, EnfileirarAutomacaoAgendamentoCriadoHandler>();
            bus.Register<OrcamentoAprovadoEvent, EnfileirarAutomacaoOrcamentoAprovadoHandler>();
            bus.Register<LancamentoCriadoEvent, EnfileirarAutomacaoLancamentoCriadoHandler>();
            return bus;
        });
    }
}
