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

        // Índices sugeridos pelo briefing §10
        builder.HasIndex(c => new { c.EstabelecimentoId, c.PacienteId })
            .HasDatabaseName("ix_cobrancas_estab_paciente");
        builder.HasIndex(c => new { c.EstabelecimentoId, c.Status })
            .HasDatabaseName("ix_cobrancas_estab_status");
        builder.HasIndex(c => c.AgendamentoId)
            .HasDatabaseName("ix_cobrancas_agendamento_id");

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
