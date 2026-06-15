using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Migracao;

/// <summary>
/// Mapping EF para migracao_jobs — aggregate root do bounded context de migração.
/// Multi-tenant: estabelecimento_id NOT NULL + FK + índice.
/// Índice em arquivo_expira_em cobre o job de expiração S3.
/// </summary>
public class MigracaoJobConfiguration : IEntityTypeConfiguration<MigracaoJob>
{
    public void Configure(EntityTypeBuilder<MigracaoJob> builder)
    {
        builder.ToTable("migracao_jobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityByDefaultColumn();

        builder.Property(j => j.EstabelecimentoId)
            .HasColumnName("estabelecimento_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(j => j.Origem)
            .HasColumnName("origem")
            .HasColumnType("varchar(200)");

        // Onda de carga: null = Onda 1 (padrão), "prontuario" = Onda 2 (CA13 — briefing 2026-06-15_001 Marco 5).
        builder.Property(j => j.Onda)
            .HasColumnName("onda")
            .HasColumnType("varchar(50)");

        builder.HasIndex(j => new { j.EstabelecimentoId, j.Onda, j.Status })
            .HasDatabaseName("ix_migracao_jobs_estab_onda_status");

        builder.Property(j => j.ArquivoS3Key)
            .HasColumnName("arquivo_s3_key")
            .HasColumnType("varchar(500)");

        builder.Property(j => j.ArquivoExpiraEm)
            .HasColumnName("arquivo_expira_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(j => j.ArquivoExpirado)
            .HasColumnName("arquivo_expirado")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(j => j.TermoAceitoEm)
            .HasColumnName("termo_aceito_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(j => j.TemplateOrigemId)
            .HasColumnName("template_origem_id")
            .HasColumnType("bigint");

        builder.Property(j => j.CriadoPorUsuarioId)
            .HasColumnName("criado_por_usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(j => j.DisparadoPorUsuarioId)
            .HasColumnName("disparado_por_usuario_id")
            .HasColumnType("uuid");

        builder.Property(j => j.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(j => j.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Addendum 002 — R-B1/R-B2: categoria da falha (sem PII) e status anterior para reprocessar.
        builder.Property(j => j.MotivoFalha)
            .HasColumnName("motivo_falha")
            .HasColumnType("text");

        builder.Property(j => j.StatusAntesFalha)
            .HasColumnName("status_antes_falha")
            .HasColumnType("text");

        // ── Índices ──────────────────────────────────────────────────────────

        // Multi-tenant por status (queries de listagem do tenant).
        builder.HasIndex(j => new { j.EstabelecimentoId, j.Status })
            .HasDatabaseName("ix_migracao_jobs_estab_status");

        // Job de expiração S3: filtra WHERE arquivo_expirado = false AND arquivo_expira_em <= NOW().
        builder.HasIndex(j => j.ArquivoExpiraEm)
            .HasDatabaseName("ix_migracao_jobs_arquivo_expira_em");

        // Listar jobs por usuário criador.
        builder.HasIndex(j => j.CriadoPorUsuarioId)
            .HasDatabaseName("ix_migracao_jobs_criado_por_usuario_id");

        // ── FKs ──────────────────────────────────────────────────────────────

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(j => j.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_migracao_jobs_estabelecimento");

        // FK fraca para template — SET NULL para preservar histórico de jobs ao excluir template.
        // HasIndex explícito garante nome snake_case (sem isso o EF gera "IX_..." no snapshot).
        builder.HasIndex(j => j.TemplateOrigemId)
            .HasDatabaseName("ix_migracao_jobs_template_origem_id");

        builder.HasOne<MigracaoTemplate>()
            .WithMany()
            .HasForeignKey(j => j.TemplateOrigemId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_migracao_jobs_template_origem");

        builder.Ignore(j => j.DomainEvents);
    }
}
