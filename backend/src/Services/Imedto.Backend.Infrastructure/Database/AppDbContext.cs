using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Auditoria;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Domain.Ia;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Salas;
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
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<ModeloDeProntuario> ModelosDeProntuario => Set<ModeloDeProntuario>();
    public DbSet<ProntuarioVariavelPool> ProntuarioVariaveisPool => Set<ProntuarioVariavelPool>();
    public DbSet<Prontuario> Prontuarios => Set<Prontuario>();
    public DbSet<ProntuarioEvolucao> ProntuarioEvolucoes => Set<ProntuarioEvolucao>();
    public DbSet<ProntuarioAcessoLog> ProntuarioAcessoLogs => Set<ProntuarioAcessoLog>();
    public DbSet<ProntuarioAnexo> ProntuarioAnexos => Set<ProntuarioAnexo>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<ItemInventario> ItensInventario => Set<ItemInventario>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<ItemOrcamento> ItensOrcamento => Set<ItemOrcamento>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<ConfiguracaoAutomacao> ConfiguracoesAutomacao => Set<ConfiguracaoAutomacao>();
    public DbSet<AuditDeleteAttempt> AuditDeleteAttempts => Set<AuditDeleteAttempt>();
    public DbSet<AiAuditLog> AiAuditLogs => Set<AiAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
