using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Estabelecimentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("agendamentos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(a => a.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(a => a.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(a => a.InicioPrevisto).HasColumnName("inicio_previsto").IsRequired();
        builder.Property(a => a.FimPrevisto).HasColumnName("fim_previsto").IsRequired();
        builder.Property(a => a.TipoServico).HasColumnName("tipo_servico").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(a => a.MotivoCancelamento).HasColumnName("motivo_cancelamento").HasMaxLength(500);
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(a => a.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(a => a.CheckInEm).HasColumnName("check_in_em");
        builder.Property(a => a.LembretePorEmailEnviado).HasColumnName("lembrete_por_email_enviado")
            .IsRequired().HasDefaultValue(false);
        builder.Property(a => a.SalaId).HasColumnName("sala_id");

        // Fase 2 — confirmação por link público
        builder.Property(a => a.TokenConfirmacao).HasColumnName("token_confirmacao").HasColumnType("text");
        builder.Property(a => a.TokenConfirmacaoExpiraEm).HasColumnName("token_confirmacao_expira_em");
        builder.Property(a => a.ConfirmadoPorLinkEm).HasColumnName("confirmado_por_link_em");

        // Índice parcial no token (lookup do endpoint público — a maioria das linhas terá NULL).
        builder.HasIndex(a => a.TokenConfirmacao)
            .IsUnique()
            .HasFilter("token_confirmacao IS NOT NULL")
            .HasDatabaseName("uq_agendamentos_token_confirmacao");

        builder.HasIndex(a => new { a.EstabelecimentoId, a.InicioPrevisto })
            .HasDatabaseName("ix_agendamentos_estab_inicio");
        builder.HasIndex(a => new { a.ProfissionalUsuarioId, a.InicioPrevisto })
            .HasDatabaseName("ix_agendamentos_prof_inicio");
        builder.HasIndex(a => new { a.PacienteId, a.InicioPrevisto })
            .HasDatabaseName("ix_agendamentos_paciente_inicio");
        builder.HasIndex(a => a.SalaId).HasDatabaseName("ix_agendamentos_sala");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(a => a.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_agendamento_estabelecimento");

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(a => a.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_agendamento_paciente");

        builder.HasOne<Sala>()
            .WithMany()
            .HasForeignKey(a => a.SalaId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_agendamento_sala");

        builder.Ignore(a => a.DomainEvents);
    }
}

public class AgendamentoConfirmacaoAcessoLogConfiguration : IEntityTypeConfiguration<AgendamentoConfirmacaoAcessoLog>
{
    public void Configure(EntityTypeBuilder<AgendamentoConfirmacaoAcessoLog> builder)
    {
        builder.ToTable("agendamento_confirmacao_acesso_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(l => l.AgendamentoId).HasColumnName("agendamento_id").IsRequired();
        builder.Property(l => l.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(l => l.IpOrigem).HasColumnName("ip_origem").HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(l => l.Acao).HasColumnName("acao").HasMaxLength(30).IsRequired();
        builder.Property(l => l.AcessadoEm).HasColumnName("acessado_em").IsRequired();

        // Lookup por agendamento (listar acessos de um agendamento específico, ordenado desc).
        builder.HasIndex(l => new { l.AgendamentoId, l.AcessadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_agendamento_confirmacao_acesso_log_agendamento_acessado");

        // Índice na FK de estabelecimento_id (rastreio multi-tenant no log).
        builder.HasIndex(l => l.EstabelecimentoId)
            .HasDatabaseName("ix_agendamento_confirmacao_acesso_log_estabelecimento");

        // FK para agendamentos — Cascade (log segue o ciclo de vida do agendamento).
        builder.HasOne<Agendamento>()
            .WithMany()
            .HasForeignKey(l => l.AgendamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_agendamento_confirmacao_acesso_log_agendamento");

        builder.HasOne<Estabelecimento>()
            .WithMany()
            .HasForeignKey(l => l.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_agendamento_confirmacao_acesso_log_estabelecimento");

        builder.Ignore(l => l.DomainEvents);
    }
}
