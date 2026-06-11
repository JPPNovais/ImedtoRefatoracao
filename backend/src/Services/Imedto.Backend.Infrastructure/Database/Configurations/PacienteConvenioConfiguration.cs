using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.PacienteConvenios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class PacienteConvenioConfiguration : IEntityTypeConfiguration<PacienteConvenio>
{
    public void Configure(EntityTypeBuilder<PacienteConvenio> builder)
    {
        builder.ToTable("paciente_convenios");
        builder.HasKey(pc => pc.Id);
        builder.Property(pc => pc.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(pc => pc.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(pc => pc.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(pc => pc.ConvenioId).HasColumnName("convenio_id").IsRequired();
        builder.Property(pc => pc.PlanoId).HasColumnName("plano_id");
        // PII: numero_carteirinha — mapeamento sem MaxLength para aceitar formatos variados de operadoras
        builder.Property(pc => pc.NumeroCarteirinha).HasColumnName("numero_carteirinha").IsRequired();
        builder.Property(pc => pc.Validade).HasColumnName("validade");
        builder.Property(pc => pc.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(pc => pc.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(pc => pc.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(pc => new { pc.PacienteId, pc.EstabelecimentoId, pc.Ativo })
            .HasDatabaseName("ix_paciente_convenios_paciente_estab_ativo");

        builder.HasIndex(pc => pc.ConvenioId)
            .HasDatabaseName("ix_paciente_convenios_convenio_id");

        // Índice explícito na FK de estabelecimento (padrão anti-FK-sem-índice).
        builder.HasIndex(pc => pc.EstabelecimentoId)
            .HasDatabaseName("ix_paciente_convenios_estabelecimento_id");

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(pc => pc.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_paciente_convenios_paciente");

        builder.HasOne<Domain.Convenios.Convenio>()
            .WithMany()
            .HasForeignKey(pc => pc.ConvenioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_paciente_convenios_convenio");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(pc => pc.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_paciente_convenios_estabelecimento");
    }
}
