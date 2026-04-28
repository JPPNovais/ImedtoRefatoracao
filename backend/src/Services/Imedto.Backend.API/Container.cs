using Microsoft.AspNetCore.Http;
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
using Imedto.Backend.Application.Orcamentos.Commands;
using Imedto.Backend.Application.Orcamentos.Events;
using Imedto.Backend.Application.Orcamentos.Queries;
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
using Imedto.Backend.Application.Usuarios.Commands;
using Imedto.Backend.Application.Usuarios.Events;
using Imedto.Backend.Application.Pacientes.Commands;
using Imedto.Backend.Application.Pacientes.Events;
using Imedto.Backend.Application.Pacientes.Queries;
using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Application.Prontuarios.Events;
using Imedto.Backend.Application.Prontuarios.Queries;
using Imedto.Backend.Application.Vinculos.Commands;
using Imedto.Backend.Application.Vinculos.Events;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
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
using Imedto.Backend.Contracts.Usuarios.Commands;
using Imedto.Backend.Application.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Pacientes.Events;
using Imedto.Backend.Domain.Profissionais.Events;
using Imedto.Backend.Domain.Prontuarios.Events;
using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.Domain.Vinculos.Events;
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
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Domain.Financeiro.Events;
using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Contracts.Automacoes.Queries;
using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Contracts.ModelosPermissao.Queries;
using Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.Infrastructure.Bus;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.Infrastructure.Ia;
using Imedto.Backend.SharedKernel.Cqrs;

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
        services.AddInfrastructure(configuration);
        RegistrarHandlers(services);
        RegistrarBuses(services);
        return services;
    }

    private static void RegistrarHandlers(IServiceCollection services)
    {
        // Usuarios
        services.AddScoped<CriarRegistroLocalUsuarioCommandHandler>();
        services.AddScoped<AtualizarPerfilUsuarioCommandHandler>();
        services.AddScoped<CompletarOnboardingUsuarioCommandHandler>();
        services.AddScoped<UsuarioCriadoEventHandler>();

        // Estabelecimentos
        services.AddScoped<CriarEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarEstabelecimentoCommandHandler>();
        services.AddScoped<AtualizarFuncionamentoCommandHandler>();
        services.AddScoped<AlterarFotoEstabelecimentoCommandHandler>();
        services.AddSingleton<ListarMeusEstabelecimentosQueryHandlers>();
        services.AddScoped<EstabelecimentoCriadoEventHandler>();
        services.AddScoped<CriarModeloPadraoAoCriarEstabelecimentoHandler>();
        services.AddScoped<CriarUnidadePadraoAoCriarEstabelecimentoHandler>();
        services.AddSingleton<EstabelecimentoQueryRepository>();

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

        // Pacientes
        services.AddScoped<CadastrarPacienteCommandHandler>();
        services.AddScoped<AtualizarPacienteCommandHandler>();
        services.AddScoped<DeletarPacienteCommandHandler>();
        services.AddSingleton<ListarPacientesQueryHandlers>();
        services.AddSingleton<ObterPacienteQueryHandlers>();
        services.AddSingleton<ExportarDadosPacienteQueryHandlers>();
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
        services.AddSingleton<ProntuarioQueryRepository>();
        services.AddScoped<ProntuarioIniciadoEventHandler>();
        services.AddScoped<EvolucaoRegistradaEventHandler>();

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
        services.AddSingleton<ListarAgendamentosQueryHandlers>();
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

        // Inventário
        services.AddScoped<CriarItemInventarioCommandHandler>();
        services.AddScoped<AtualizarItemInventarioCommandHandler>();
        services.AddScoped<RegistrarMovimentacaoEstoqueCommandHandler>();
        services.AddScoped<InativarItemInventarioCommandHandler>();
        services.AddSingleton<ListarItensInventarioQueryHandlers>();
        services.AddSingleton<ListarMovimentacoesQueryHandlers>();
        services.AddSingleton<InventarioQueryRepository>();
        services.AddScoped<EstoqueAbaixoMinimoEventHandler>();

        // Orçamentos
        services.AddScoped<CriarOrcamentoCommandHandler>();
        services.AddScoped<AtualizarOrcamentoCommandHandler>();
        services.AddScoped<AprovarOrcamentoCommandHandler>();
        services.AddScoped<RecusarOrcamentoCommandHandler>();
        services.AddSingleton<ListarOrcamentosQueryHandlers>();
        services.AddSingleton<ObterOrcamentoQueryHandlers>();
        services.AddSingleton<OrcamentoQueryRepository>();
        services.AddScoped<OrcamentoCriadoEventHandler>();
        services.AddScoped<OrcamentoAprovadoEventHandler>();

        // Financeiro
        services.AddScoped<CriarLancamentoCommandHandler>();
        services.AddScoped<AtualizarLancamentoCommandHandler>();
        services.AddScoped<PagarLancamentoCommandHandler>();
        services.AddScoped<CancelarLancamentoCommandHandler>();
        services.AddSingleton<ListarLancamentosQueryHandlers>();
        services.AddSingleton<ObterResumoFinanceiroQueryHandlers>();
        services.AddSingleton<FinanceiroQueryRepository>();
        services.AddScoped<LancamentoCriadoEventHandler>();
        services.AddScoped<LancamentoPagoEventHandler>();

        // Automações
        services.AddScoped<ExpirarOrcamentosVencidosCommandHandler>();
        services.AddScoped<EnviarLembretesAgendamentosCommandHandler>();
        services.AddScoped<SalvarConfiguracaoAutomacaoCommandHandler>();
        services.AddSingleton<ObterConfiguracaoAutomacaoQueryHandlers>();
        services.AddScoped<IConfiguracaoAutomacaoRepository, ConfiguracaoAutomacaoRepository>();
        services.AddSingleton<IEmailService, ResendEmailService>();
        services.AddScoped<IIaService, AnthropicIaService>();

        // Dashboard & Relatórios
        services.AddSingleton<DashboardQueryHandlers>();
        services.AddSingleton<DashboardQueryRepository>();
        services.AddSingleton<RelatorioFaturamentoQueryHandlers>();
        services.AddSingleton<RelatorioAgendamentosQueryHandlers>();
        services.AddSingleton<RelatorioQueryRepository>();
    }

    private static void RegistrarBuses(IServiceCollection services)
    {
        services.AddSingleton<ICommandBus>(sp =>
        {
            var bus = new MemoryCommandBus(sp, sp.GetRequiredService<IHttpContextAccessor>());
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
            bus.Register<CriarItemInventarioCommand, CriarItemInventarioCommandHandler>();
            bus.Register<AtualizarItemInventarioCommand, AtualizarItemInventarioCommandHandler>();
            bus.Register<RegistrarMovimentacaoEstoqueCommand, RegistrarMovimentacaoEstoqueCommandHandler>();
            bus.Register<InativarItemInventarioCommand, InativarItemInventarioCommandHandler>();
            bus.Register<CriarOrcamentoCommand, CriarOrcamentoCommandHandler>();
            bus.Register<AtualizarOrcamentoCommand, AtualizarOrcamentoCommandHandler>();
            bus.Register<AprovarOrcamentoCommand, AprovarOrcamentoCommandHandler>();
            bus.Register<RecusarOrcamentoCommand, RecusarOrcamentoCommandHandler>();
            bus.Register<CriarLancamentoCommand, CriarLancamentoCommandHandler>();
            bus.Register<AtualizarLancamentoCommand, AtualizarLancamentoCommandHandler>();
            bus.Register<PagarLancamentoCommand, PagarLancamentoCommandHandler>();
            bus.Register<CancelarLancamentoCommand, CancelarLancamentoCommandHandler>();
            bus.Register<ExpirarOrcamentosVencidosCommand, ExpirarOrcamentosVencidosCommandHandler>();
            bus.Register<EnviarLembretesAgendamentosCommand, EnviarLembretesAgendamentosCommandHandler>();
            bus.Register<SalvarConfiguracaoAutomacaoCommand, SalvarConfiguracaoAutomacaoCommandHandler>();
            return bus;
        });

        services.AddSingleton<IRequestBus>(sp =>
        {
            var bus = new MemoryRequestBus(sp, sp.GetRequiredService<IHttpContextAccessor>());
            bus.Register<ListarMeusEstabelecimentosQuery, IEnumerable<EstabelecimentoDto>, ListarMeusEstabelecimentosQueryHandlers>();
            bus.Register<ListarUnidadesQuery, IEnumerable<UnidadeDto>, ListarUnidadesQueryHandlers>();
            bus.Register<ListarSalasQuery, IEnumerable<SalaDto>, ListarSalasQueryHandlers>();
            bus.Register<ListarTiposSalaQuery, IEnumerable<TipoSalaDto>, ListarTiposSalaQueryHandlers>();
            bus.Register<ObterProfissionalMeQuery, ProfissionalDto, ObterProfissionalMeQueryHandlers>();
            bus.Register<ListarProfissionaisEstabelecimentoQuery, IEnumerable<ProfissionalVinculadoDto>, ListarProfissionaisEstabelecimentoQueryHandlers>();
            bus.Register<ListarMeusConvitesQuery, IEnumerable<ConviteDto>, ListarMeusConvitesQueryHandlers>();
            bus.Register<ListarPacientesQuery, PaginaPacientesDto, ListarPacientesQueryHandlers>();
            bus.Register<ObterPacienteQuery, PacienteDto, ObterPacienteQueryHandlers>();
            bus.Register<ExportarDadosPacienteQuery, object, ExportarDadosPacienteQueryHandlers>();
            bus.Register<ListarModelosDisponiveisQuery, IEnumerable<ModeloProntuarioDto>, ListarModelosDisponiveisQueryHandlers>();
            bus.Register<ObterModeloDeProntuarioQuery, ModeloProntuarioDto, ObterModeloDeProntuarioQueryHandlers>();
            bus.Register<ListarVariaveisPoolQuery, IEnumerable<VariavelPoolDto>, ListarVariaveisPoolQueryHandlers>();
            bus.Register<ObterProntuarioDoPacienteQuery, ProntuarioCompletoDto, ObterProntuarioDoPacienteQueryHandlers>();
            bus.Register<ListarAnexosDoProntuarioQuery, IEnumerable<AnexoDto>, ListarAnexosDoProntuarioQueryHandlers>();
            bus.Register<ObterUrlAnexoQuery, AnexoUrlDto, ObterUrlAnexoQueryHandlers>();
            bus.Register<ListarModelosPermissaoQuery, IEnumerable<ModeloPermissaoDto>, ListarModelosPermissaoQueryHandlers>();
            bus.Register<ListarAgendamentosQuery, IEnumerable<AgendamentoDto>, ListarAgendamentosQueryHandlers>();
            bus.Register<ObterAgendamentoQuery, AgendamentoDto, ObterAgendamentoQueryHandlers>();
            bus.Register<ConsultarDisponibilidadeQuery, DisponibilidadeSemanaDto, ConsultarDisponibilidadeQueryHandlers>();
            bus.Register<ListarItensInventarioQuery, IEnumerable<ItemInventarioDto>, ListarItensInventarioQueryHandlers>();
            bus.Register<ListarMovimentacoesQuery, IEnumerable<MovimentacaoEstoqueDto>, ListarMovimentacoesQueryHandlers>();
            bus.Register<ListarOrcamentosQuery, IEnumerable<OrcamentoResumoDto>, ListarOrcamentosQueryHandlers>();
            bus.Register<ObterOrcamentoQuery, OrcamentoDto, ObterOrcamentoQueryHandlers>();
            bus.Register<ListarLancamentosQuery, IEnumerable<LancamentoDto>, ListarLancamentosQueryHandlers>();
            bus.Register<ObterResumoFinanceiroQuery, ResumoFinanceiroDto, ObterResumoFinanceiroQueryHandlers>();
            bus.Register<DashboardQuery, DashboardDto, DashboardQueryHandlers>();
            bus.Register<RelatorioFaturamentoQuery, IEnumerable<FaturamentoCategoriaDto>, RelatorioFaturamentoQueryHandlers>();
            bus.Register<RelatorioAgendamentosQuery, RelatorioAgendamentosDto, RelatorioAgendamentosQueryHandlers>();
            bus.Register<ObterConfiguracaoAutomacaoQuery, ConfiguracaoAutomacaoDto, ObterConfiguracaoAutomacaoQueryHandlers>();
            return bus;
        });

        services.AddSingleton<IEventBus>(sp =>
        {
            var bus = new MemoryEventBus(sp, sp.GetRequiredService<IHttpContextAccessor>());
            bus.Register<UsuarioCriadoEvent, UsuarioCriadoEventHandler>();
            bus.Register<EstabelecimentoCriadoEvent, EstabelecimentoCriadoEventHandler>();
            bus.Register<EstabelecimentoCriadoEvent, CriarModeloPadraoAoCriarEstabelecimentoHandler>();
            bus.Register<EstabelecimentoCriadoEvent, CriarUnidadePadraoAoCriarEstabelecimentoHandler>();
            bus.Register<ProfissionalCadastradoEvent, ProfissionalCadastradoEventHandler>();
            bus.Register<ProfissionalConvidadoEvent, ProfissionalConvidadoEventHandler>();
            bus.Register<VinculoAceitoEvent, VinculoAceitoEventHandler>();
            bus.Register<PacienteCadastradoEvent, PacienteCadastradoEventHandler>();
            bus.Register<ProntuarioIniciadoEvent, ProntuarioIniciadoEventHandler>();
            bus.Register<EvolucaoRegistradaEvent, EvolucaoRegistradaEventHandler>();
            bus.Register<AgendamentoCriadoEvent, AgendamentoCriadoEventHandler>();
            bus.Register<AgendamentoCanceladoEvent, AgendamentoCanceladoEventHandler>();
            bus.Register<EstoqueAbaixoMinimoEvent, EstoqueAbaixoMinimoEventHandler>();
            bus.Register<OrcamentoCriadoEvent, OrcamentoCriadoEventHandler>();
            bus.Register<OrcamentoAprovadoEvent, OrcamentoAprovadoEventHandler>();
            bus.Register<LancamentoCriadoEvent, LancamentoCriadoEventHandler>();
            bus.Register<LancamentoPagoEvent, LancamentoPagoEventHandler>();
            return bus;
        });
    }
}
