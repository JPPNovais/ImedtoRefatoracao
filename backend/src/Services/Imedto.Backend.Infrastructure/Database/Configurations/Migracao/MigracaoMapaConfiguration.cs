using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Migracao;

/// <summary>
/// Mapping EF para migracao_mapas — mapeamento de campos por entidade por job.
/// UNIQUE (migracao_job_id, entidade, nome_bloco_origem) — permite múltiplos blocos
/// do mesmo dump JSON classificados na mesma entidade canônica.
/// CASCADE delete: mapas deletados quando job é excluído.
/// </summary>
public class MigracaoMapaConfiguration : IEntityTypeConfiguration<MigracaoMapa>
{
    public void Configure(EntityTypeBuilder<MigracaoMapa> builder)
    {
        builder.ToTable("migracao_mapas");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityByDefaultColumn();

        builder.Property(m => m.MigracaoJobId)
            .HasColumnName("migracao_job_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(m => m.EstabelecimentoId)
            .HasColumnName("estabelecimento_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(m => m.Entidade)
            .HasColumnName("entidade")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(m => m.NomeBlocoOrigem)
            .HasColumnName("nome_bloco_origem")
            .HasColumnType("text")
            .IsRequired()
            .HasDefaultValue(string.Empty);

        builder.Property(m => m.MapaJson)
            .HasColumnName("mapa_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.RevisadoPorUsuarioId)
            .HasColumnName("revisado_por_usuario_id")
            .HasColumnType("uuid");

        builder.Property(m => m.RevisadoEm)
            .HasColumnName("revisado_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(m => m.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────

        // UNIQUE — 1 mapa por (job, entidade, bloco de origem).
        // Permite múltiplos blocos do mesmo dump classificados na mesma entidade canônica.
        builder.HasIndex(m => new { m.MigracaoJobId, m.Entidade, m.NomeBlocoOrigem })
            .IsUnique()
            .HasDatabaseName("uq_migracao_mapas_job_entidade_bloco");

        // Índice de suporte para filtro/ordenação por bloco no painel de revisão.
        builder.HasIndex(m => new { m.MigracaoJobId, m.NomeBlocoOrigem })
            .HasDatabaseName("ix_migracao_mapas_job_bloco");

        // ── FK ───────────────────────────────────────────────────────────────

        builder.HasOne<MigracaoJob>()
            .WithMany()
            .HasForeignKey(m => m.MigracaoJobId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_migracao_mapas_job");

        builder.Ignore(m => m.DomainEvents);
    }
}
