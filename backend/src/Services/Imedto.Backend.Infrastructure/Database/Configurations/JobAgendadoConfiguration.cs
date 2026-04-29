using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Jobs;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class JobAgendadoConfiguration : IEntityTypeConfiguration<JobAgendado>
{
    public void Configure(EntityTypeBuilder<JobAgendado> builder)
    {
        builder.ToTable("jobs_agendados");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(j => j.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        builder.Property(j => j.ProximoRunEm).HasColumnName("proximo_run_em").IsRequired();
        builder.Property(j => j.UltimoRunEm).HasColumnName("ultimo_run_em");
        builder.Property(j => j.IntervaloSeg).HasColumnName("intervalo_seg").IsRequired();
        builder.Property(j => j.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(j => j.UltimaFalha).HasColumnName("ultima_falha").HasMaxLength(500);
        builder.Property(j => j.Tentativas).HasColumnName("tentativas").IsRequired();
        builder.Property(j => j.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(j => j.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(j => j.Nome)
            .IsUnique()
            .HasDatabaseName("uq_jobs_agendados_nome");

        // Índice usado pelo scheduler na query "prontos para executar".
        builder.HasIndex(j => new { j.Status, j.ProximoRunEm })
            .HasDatabaseName("ix_jobs_agendados_status_proximo_run");

        builder.Ignore(j => j.DomainEvents);
    }
}
