using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.AssinaturaDigital;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.Atestados;
using Imedto.Backend.Domain.Auth;
using Imedto.Backend.Domain.Auditoria;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Catalogo;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.Domain.Idempotency;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Lgpd;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.PedidosExame;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;

namespace Imedto.Backend.Infrastructure.Database;

/// <summary>
/// DbContext raiz. DbSets de aggregates são adicionados conforme os domínios são migrados do legado.
/// Entity configurations vivem em Database/Configurations/ e são descobertas por reflexão.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Estabelecimento> Estabelecimentos => Set<Estabelecimento>();
    public DbSet<UnidadeEstabelecimento> Unidades => Set<UnidadeEstabelecimento>();
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<TipoSala> TiposSala => Set<TipoSala>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<ModeloPermissaoEstabelecimento> ModelosPermissao => Set<ModeloPermissaoEstabelecimento>();
    public DbSet<VinculoProfissionalEstabelecimento> Vinculos => Set<VinculoProfissionalEstabelecimento>();
    public DbSet<SolicitacaoVinculo> SolicitacoesVinculo => Set<SolicitacaoVinculo>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<PacienteAcessoLog> PacienteAcessoLogs => Set<PacienteAcessoLog>();
    public DbSet<ModeloDeProntuario> ModelosDeProntuario => Set<ModeloDeProntuario>();
    public DbSet<ProntuarioVariavelPool> ProntuarioVariaveisPool => Set<ProntuarioVariavelPool>();
    public DbSet<Prontuario> Prontuarios => Set<Prontuario>();
    public DbSet<ProntuarioEvolucao> ProntuarioEvolucoes => Set<ProntuarioEvolucao>();
    public DbSet<ProntuarioAcessoLog> ProntuarioAcessoLogs => Set<ProntuarioAcessoLog>();
    public DbSet<ProntuarioAnexo> ProntuarioAnexos => Set<ProntuarioAnexo>();
    public DbSet<ExameFisico> ExamesFisicos => Set<ExameFisico>();
    public DbSet<RegiaoExameFisico> RegioesExameFisico => Set<RegiaoExameFisico>();
    public DbSet<Receita> Receitas => Set<Receita>();
    public DbSet<ItemReceita> ReceitaItens => Set<ItemReceita>();
    public DbSet<Atestado> Atestados => Set<Atestado>();
    public DbSet<ModeloAtestado> ModelosAtestado => Set<ModeloAtestado>();
    public DbSet<PedidoExame> PedidosExame => Set<PedidoExame>();
    public DbSet<ConfiguracaoReceitaEstabelecimento> ConfiguracoesReceita => Set<ConfiguracaoReceitaEstabelecimento>();
    public DbSet<MedicamentoFavorito> MedicamentosFavoritos => Set<MedicamentoFavorito>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<AgendamentoSalaAudit> AgendamentoSalaAudits => Set<AgendamentoSalaAudit>();
    public DbSet<AgendamentoConfirmacaoAcessoLog> AgendamentoConfirmacaoAcessoLogs => Set<AgendamentoConfirmacaoAcessoLog>();
    public DbSet<ListaEsperaAgendamento> ListaEsperaAgendamentos => Set<ListaEsperaAgendamento>();
    public DbSet<ItemInventario> ItensInventario => Set<ItemInventario>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<CategoriaEstoque> CategoriasEstoque => Set<CategoriaEstoque>();
    public DbSet<FabricanteEstoque> FabricantesEstoque => Set<FabricanteEstoque>();
    public DbSet<FornecedorEstoque> FornecedoresEstoque => Set<FornecedorEstoque>();
    public DbSet<LocalEstoque> LocaisEstoque => Set<LocalEstoque>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<ItemOrcamento> ItensOrcamento => Set<ItemOrcamento>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<CategoriaFinanceira> CategoriasFinanceiras => Set<CategoriaFinanceira>();
    public DbSet<FormaPagamento> FormasPagamento => Set<FormaPagamento>();
    public DbSet<ConfiguracaoAutomacao> ConfiguracoesAutomacao => Set<ConfiguracaoAutomacao>();
    public DbSet<RegraAutomacao> RegrasAutomacao => Set<RegraAutomacao>();
    public DbSet<EventoAutomacao> EventosAutomacao => Set<EventoAutomacao>();
    public DbSet<AuditDeleteAttempt> AuditDeleteAttempts => Set<AuditDeleteAttempt>();
    public DbSet<AiAuditLog> AiAuditLogs => Set<AiAuditLog>();
    public DbSet<EstabelecimentoIaSettings> EstabelecimentosIaSettings => Set<EstabelecimentoIaSettings>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<Profissao> Profissoes => Set<Profissao>();
    public DbSet<Especialidade> Especialidades => Set<Especialidade>();
    public DbSet<RegiaoAnatomicaCatalogo> RegioesAnatomicasCatalogo => Set<RegiaoAnatomicaCatalogo>();
    public DbSet<JobAgendado> JobsAgendados => Set<JobAgendado>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Plano> Planos => Set<Plano>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<ProcedimentoCirurgico> ProcedimentosCirurgicos => Set<ProcedimentoCirurgico>();
    public DbSet<MembroEquipeCirurgica> MembrosEquipeCirurgica => Set<MembroEquipeCirurgica>();
    public DbSet<OrcamentoEquipe> OrcamentoEquipe => Set<OrcamentoEquipe>();
    public DbSet<OrcamentoImplante> OrcamentoImplantes => Set<OrcamentoImplante>();
    public DbSet<OrcamentoFormaPagamento> OrcamentoFormasPagamento => Set<OrcamentoFormaPagamento>();
    // Item 6 — cirurgias múltiplas + local cirúrgico (snapshot embutido no Orcamento) + anestesia.
    public DbSet<OrcamentoCirurgia> OrcamentoCirurgias => Set<OrcamentoCirurgia>();
    public DbSet<OrcamentoAnestesia> OrcamentoAnestesias => Set<OrcamentoAnestesia>();

    // Fase 6.1 — Catálogos de orçamento (settings).
    public DbSet<CatalogoCirurgia> CatalogoCirurgias => Set<CatalogoCirurgia>();
    public DbSet<ValorProfissionalOrcamento> ValoresProfissionalOrcamento => Set<ValorProfissionalOrcamento>();
    public DbSet<ConfiguracaoLocalCirurgia> ConfiguracoesLocalCirurgia => Set<ConfiguracaoLocalCirurgia>();
    public DbSet<CatalogoEquipeEspecializada> CatalogoEquipesEspecializadas => Set<CatalogoEquipeEspecializada>();
    public DbSet<CatalogoImplante> CatalogoImplantes => Set<CatalogoImplante>();
    public DbSet<ConfiguracaoPagamentoCatalogo> ConfiguracoesPagamentoCatalogo => Set<ConfiguracaoPagamentoCatalogo>();
    public DbSet<CatalogoProduto> CatalogoProdutos => Set<CatalogoProduto>();
    public DbSet<CatalogoCirurgiaProduto> CatalogoCirurgiaProdutos => Set<CatalogoCirurgiaProduto>();
    // Config-orcamento 2026-05-16 — design ConfigOrcamento (5 abas).
    public DbSet<OrcamentoTeamRole> OrcamentoTeamRoles => Set<OrcamentoTeamRole>();
    public DbSet<OrcamentoAnestesista> OrcamentoAnestesistas => Set<OrcamentoAnestesista>();
    public DbSet<OrcamentoAnestesistaFaixa> OrcamentoAnestesistaFaixas => Set<OrcamentoAnestesistaFaixa>();
    public DbSet<OrcamentoPacote> OrcamentoPacotes => Set<OrcamentoPacote>();
    public DbSet<OrcamentoPacoteProcedimento> OrcamentoPacoteProcedimentos => Set<OrcamentoPacoteProcedimento>();
    public DbSet<OrcamentoPacoteProduto> OrcamentoPacoteProdutos => Set<OrcamentoPacoteProduto>();
    public DbSet<OrcamentoPacoteTeamRole> OrcamentoPacoteTeamRoles => Set<OrcamentoPacoteTeamRole>();


    // Item 4.3 — LGPD: anonimizações.
    public DbSet<LgpdAnonimizacao> LgpdAnonimizacoes => Set<LgpdAnonimizacao>();

    // Item 4.13 — Catálogo TUSS/CBHPM de procedimentos.
    public DbSet<ProcedimentoCatalogo> CatalogoProcedimentos => Set<ProcedimentoCatalogo>();

    // Auth local (JWT) — credencial 1:1 com Usuario.
    public DbSet<AuthCredencial> AuthCredenciais => Set<AuthCredencial>();
    public DbSet<AuthRefreshToken> AuthRefreshTokens => Set<AuthRefreshToken>();
    public DbSet<AuthEmailToken> AuthEmailTokens => Set<AuthEmailToken>();

    // 2FA TOTP por usuário (briefing 2026-06-10_006).
    public DbSet<Usuario2fa> Usuario2fas => Set<Usuario2fa>();
    public DbSet<Usuario2faCodigoRecuperacao> Usuario2faCodigosRecuperacao => Set<Usuario2faCodigoRecuperacao>();
    public DbSet<UsuarioSegurancaAudit> UsuarioSegurancaAudits => Set<UsuarioSegurancaAudit>();

    // Área Admin Global (2026-05-30).
    public DbSet<ImedtoAdmin> ImedtoAdmins => Set<ImedtoAdmin>();
    public DbSet<ImedtoAdminRefreshToken> ImedtoAdminRefreshTokens => Set<ImedtoAdminRefreshToken>();
    public DbSet<ImedtoAdminAuditLog> ImedtoAdminAuditLogs => Set<ImedtoAdminAuditLog>();
    public DbSet<ImedtoPlano> ImedtoPlanos => Set<ImedtoPlano>();
    public DbSet<ImedtoAssinatura> ImedtoAssinaturas => Set<ImedtoAssinatura>();
    public DbSet<ImedtoConfig> ImedtoConfigs => Set<ImedtoConfig>();

    // Assinatura Digital ICP-Brasil (2026-06-01).
    public DbSet<AssinaturaCertificado> AssinaturaCertificados => Set<AssinaturaCertificado>();
    public DbSet<AssinaturaAuditLog> AssinaturaAuditLogs => Set<AssinaturaAuditLog>();

    // Termos de consentimento (Fase 1 — 2026-05-19).
    public DbSet<TermoModelo> TermosModelo => Set<TermoModelo>();
    public DbSet<TermoModeloVersao> TermosModeloVersao => Set<TermoModeloVersao>();
    public DbSet<TermoEmitido> TermosEmitidos => Set<TermoEmitido>();
    public DbSet<TermoAuditLog> TermosAuditLog => Set<TermoAuditLog>();
    public DbSet<TermoEmitidoAcessoLog> TermosEmitidoAcessoLog => Set<TermoEmitidoAcessoLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
