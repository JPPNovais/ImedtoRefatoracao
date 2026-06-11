using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class CaixaDiarioConfiguration : IEntityTypeConfiguration<CaixaDiario>
{
    public void Configure(EntityTypeBuilder<CaixaDiario> builder)
    {
        builder.ToTable("caixa_diario");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.Data).HasColumnName("data").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.AbertoPorUsuarioId).HasColumnName("aberto_por_usuario_id").IsRequired();
        builder.Property(c => c.AbertoEm).HasColumnName("aberto_em").IsRequired();

        builder.Property(c => c.FechadoPorUsuarioId).HasColumnName("fechado_por_usuario_id");
        builder.Property(c => c.FechadoEm).HasColumnName("fechado_em");
        builder.Property(c => c.Observacao).HasColumnName("observacao").HasMaxLength(500);

        builder.Property(c => c.ReabertoPorUsuarioId).HasColumnName("reaberto_por_usuario_id");
        builder.Property(c => c.ReabertoEm).HasColumnName("reaberto_em");

        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        // UNIQUE (estabelecimento_id, data) — 1 caixa por dia por tenant (R6).
        builder.HasIndex(c => new { c.EstabelecimentoId, c.Data })
            .IsUnique()
            .HasDatabaseName("uq_caixa_diario_estab_data");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_caixa_diario_estabelecimento");

        builder.Ignore(c => c.DomainEvents);
    }
}
