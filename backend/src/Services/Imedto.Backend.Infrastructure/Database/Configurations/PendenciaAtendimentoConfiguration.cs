using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios.Pendencias;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class PendenciaAtendimentoConfiguration : IEntityTypeConfiguration<PendenciaAtendimento>
{
    public void Configure(EntityTypeBuilder<PendenciaAtendimento> builder)
    {
        builder.ToTable("pendencias_atendimento");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(p => p.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(p => p.EvolucaoId).HasColumnName("evolucao_id").IsRequired();
        builder.Property(p => p.AgendamentoId).HasColumnName("agendamento_id");
        builder.Property(p => p.Acao)
            .HasColumnName("acao")
            .HasMaxLength(40)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(p => p.ReferenciaId).HasColumnName("referencia_id");
        builder.Property(p => p.ConcluidaEm).HasColumnName("concluida_em");
        builder.Property(p => p.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(p => p.DomainEvents);

        // Idempotência na criação (R3/CA62)
        builder.HasIndex(p => new { p.EvolucaoId, p.Acao })
            .IsUnique()
            .HasDatabaseName("uq_pendencias_evolucao_acao");

        // Índice de leitura do painel (CA74)
        builder.HasIndex(p => new { p.EstabelecimentoId, p.PacienteId, p.Status })
            .HasDatabaseName("ix_pendencias_estab_paciente_status");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(p => p.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pendencias_estabelecimento");

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(p => p.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pendencias_paciente");

        builder.HasOne<Domain.Prontuarios.ProntuarioEvolucao>()
            .WithMany()
            .HasForeignKey(p => p.EvolucaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pendencias_evolucao");
    }
}
