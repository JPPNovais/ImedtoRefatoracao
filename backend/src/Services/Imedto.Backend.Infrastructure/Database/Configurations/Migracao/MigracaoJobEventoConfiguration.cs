using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Migracao;

/// <summary>
/// Mapping EF para migracao_job_eventos — trilha de transições de status de um MigracaoJob.
/// Tabela forward-only (sem PII), multi-tenant via estabelecimento_id herdado do job.
/// (addendum 003 — briefing 2026-06-15_004)
/// </summary>
public class MigracaoJobEventoConfiguration : IEntityTypeConfiguration<MigracaoJobEvento>
{
    public void Configure(EntityTypeBuilder<MigracaoJobEvento> builder)
    {
        builder.ToTable("migracao_job_eventos");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityByDefaultColumn();

        builder.Property(e => e.MigracaoJobId)
            .HasColumnName("migracao_job_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.EstabelecimentoId)
            .HasColumnName("estabelecimento_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.StatusAnterior)
            .HasColumnName("status_anterior")
            .HasColumnType("text");

        builder.Property(e => e.StatusNovo)
            .HasColumnName("status_novo")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.UsuarioId)
            .HasColumnName("usuario_id")
            .HasColumnType("uuid");

        builder.Property(e => e.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────

        // Timeline cronológica de um job — query principal.
        builder.HasIndex(e => new { e.MigracaoJobId, e.CriadoEm })
            .HasDatabaseName("ix_migracao_job_eventos_job_criado_em");

        // ── FK ───────────────────────────────────────────────────────────────

        // CASCADE: ao excluir o job, todos os eventos são removidos junto.
        builder.HasOne<MigracaoJob>()
            .WithMany()
            .HasForeignKey(e => e.MigracaoJobId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_migracao_job_eventos_job");
    }
}
