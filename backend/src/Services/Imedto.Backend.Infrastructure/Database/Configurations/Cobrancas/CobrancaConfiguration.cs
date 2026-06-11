using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cobrancas;

public class CobrancaConfiguration : IEntityTypeConfiguration<Cobranca>
{
    public void Configure(EntityTypeBuilder<Cobranca> builder)
    {
        builder.ToTable("cobrancas");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(c => c.Origem).HasColumnName("origem").HasMaxLength(50).IsRequired();
        builder.Property(c => c.AgendamentoId).HasColumnName("agendamento_id");
        builder.Property(c => c.OrcamentoId).HasColumnName("orcamento_id");
        builder.Property(c => c.EvolucaoId).HasColumnName("evolucao_id");
        builder.Property(c => c.TipoAtendimento).HasColumnName("tipo_atendimento").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(c => c.ConvenioId).HasColumnName("convenio_id");
        builder.Property(c => c.ValorCobrado).HasColumnName("valor_cobrado").HasPrecision(12, 2).IsRequired();
        builder.Property(c => c.Desconto).HasColumnName("desconto").HasPrecision(12, 2).IsRequired()
            .HasDefaultValue(0m);
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(c => c.Descricao).HasColumnName("descricao").HasMaxLength(300);
        builder.Property(c => c.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(c => c.DomainEvents);

        // Índices operacionais
        builder.HasIndex(c => new { c.EstabelecimentoId, c.PacienteId })
            .HasDatabaseName("ix_cobrancas_estab_paciente");
        builder.HasIndex(c => new { c.EstabelecimentoId, c.Status })
            .HasDatabaseName("ix_cobrancas_estab_status");
        builder.HasIndex(c => c.AgendamentoId)
            .HasDatabaseName("ix_cobrancas_agendamento_id");

        // Índice UNIQUE parcial — idempotência F4 (R7/CA77/CA78):
        // garante 1 cobrança de procedimento por evolução (defense-in-depth contra race).
        builder.HasIndex(c => c.EvolucaoId)
            .IsUnique()
            .HasDatabaseName("ux_cobrancas_evolucao_procedimento")
            .HasFilter("origem = 'Procedimento' AND evolucao_id IS NOT NULL");

        // Índice UNIQUE parcial — idempotência F5 (R6/CA104):
        // garante 1 cobrança de cirurgia por orçamento (defense-in-depth contra race).
        // Nota: filter com literal string pode exigir migrationBuilder.Sql para criação correta.
        // O imedto-database executa via SQL idempotente em db/migrations/.
        builder.HasIndex(c => c.OrcamentoId)
            .IsUnique()
            .HasDatabaseName("ux_cobrancas_orcamento_cirurgia")
            .HasFilter("origem = 'Cirurgia' AND orcamento_id IS NOT NULL");

        builder.HasMany(c => c.Pagamentos)
            .WithOne()
            .HasForeignKey(p => p.CobrancaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pagamentos_cobranca");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cobrancas_estabelecimento");
    }
}
