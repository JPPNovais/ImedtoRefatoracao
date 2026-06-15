using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Migracao;

/// <summary>
/// Mapping EF para migracao_registros — filho do MigracaoJob.
/// CASCADE delete: registros deletados quando job é excluído.
/// estabelecimento_id redundante cobre queries multi-tenant sem JOIN.
/// payload_bruto é JSONB — não logar, pode conter PII do tenant.
/// </summary>
public class MigracaoRegistroConfiguration : IEntityTypeConfiguration<MigracaoRegistro>
{
    public void Configure(EntityTypeBuilder<MigracaoRegistro> builder)
    {
        builder.ToTable("migracao_registros");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityByDefaultColumn();

        builder.Property(r => r.MigracaoJobId)
            .HasColumnName("migracao_job_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(r => r.EstabelecimentoId)
            .HasColumnName("estabelecimento_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(r => r.Entidade)
            .HasColumnName("entidade")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(r => r.PayloadBruto)
            .HasColumnName("payload_bruto")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .IsRequired()
            .HasDefaultValue("pendente");

        builder.Property(r => r.MotivoRejeicao)
            .HasColumnName("motivo_rejeicao")
            .HasColumnType("text");

        builder.Property(r => r.EntidadeAlvoId)
            .HasColumnName("entidade_alvo_id")
            .HasColumnType("bigint");

        builder.Property(r => r.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────

        // Relatório de status por job (progresso e erros).
        builder.HasIndex(r => new { r.MigracaoJobId, r.Status })
            .HasDatabaseName("ix_migracao_registros_job_status");

        // Queries multi-tenant por tipo de entidade (listagem/contagem).
        builder.HasIndex(r => new { r.EstabelecimentoId, r.Entidade })
            .HasDatabaseName("ix_migracao_registros_estab_entidade");

        // FK coverage para migracao_job_id.
        builder.HasIndex(r => r.MigracaoJobId)
            .HasDatabaseName("ix_migracao_registros_job_id");

        // ── FK ───────────────────────────────────────────────────────────────

        builder.HasOne<MigracaoJob>()
            .WithMany()
            .HasForeignKey(r => r.MigracaoJobId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_migracao_registros_job");

        builder.Ignore(r => r.DomainEvents);
    }
}
