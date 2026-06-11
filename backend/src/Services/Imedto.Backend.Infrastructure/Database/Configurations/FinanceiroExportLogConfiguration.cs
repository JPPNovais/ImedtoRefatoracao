using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;


/// <summary>
/// Mapping EF Core para <see cref="FinanceiroExportLog"/>.
/// Tabela append-only: sem colunas de atualização. FK para estabelecimentos RESTRICT.
/// Índice composto (estabelecimento_id, ocorrido_em) cobre relatórios de auditoria por tenant.
/// </summary>
public class FinanceiroExportLogConfiguration : IEntityTypeConfiguration<FinanceiroExportLog>
{
    public void Configure(EntityTypeBuilder<FinanceiroExportLog> builder)
    {
        builder.ToTable("financeiro_export_log");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityAlwaysColumn();

        builder.Property(e => e.UsuarioId)
            .HasColumnName("usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.EstabelecimentoId)
            .HasColumnName("estabelecimento_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(e => e.Acao)
            .HasColumnName("acao")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.PeriodoInicio)
            .HasColumnName("periodo_inicio")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.PeriodoFim)
            .HasColumnName("periodo_fim")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.TotalLinhas)
            .HasColumnName("total_linhas")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(e => e.OcorridoEm)
            .HasColumnName("ocorrido_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // FK para estabelecimentos: RESTRICT (audit log preservado se o estab for deletado via soft-delete).
        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(e => e.EstabelecimentoId)
            .HasConstraintName("fk_financeiro_export_log_estabelecimento")
            .OnDelete(DeleteBehavior.Restrict);

        // Índice composto: relatórios de auditoria por tenant ordenados por data.
        builder.HasIndex(e => new { e.EstabelecimentoId, e.OcorridoEm })
            .HasDatabaseName("ix_financeiro_export_log_estabelecimento_data");

        builder.Ignore(e => e.DomainEvents);
    }
}
