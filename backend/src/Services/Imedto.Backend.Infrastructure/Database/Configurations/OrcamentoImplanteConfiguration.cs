using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoImplanteConfiguration : IEntityTypeConfiguration<OrcamentoImplante>
{
    public void Configure(EntityTypeBuilder<OrcamentoImplante> builder)
    {
        builder.ToTable("orcamento_implantes");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(i => i.OrcamentoId).HasColumnName("orcamento_id").IsRequired();
        builder.Property(i => i.ItemInventarioId).HasColumnName("item_inventario_id");
        builder.Property(i => i.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasPrecision(12, 3).IsRequired();
        builder.Property(i => i.CustoUnitario).HasColumnName("custo_unitario").HasPrecision(18, 4).IsRequired();
        builder.Property(i => i.CustoTotal).HasColumnName("custo_total").HasPrecision(18, 4).IsRequired();

        builder.Ignore(i => i.DomainEvents);

        // FK opcional para o catálogo. SET NULL preserva o snapshot do orçamento se o item
        // for removido do inventário.
        builder.HasOne<Domain.Inventario.ItemInventario>()
            .WithMany()
            .HasForeignKey(i => i.ItemInventarioId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_orcamento_implante_item_inventario");

        builder.HasIndex(i => i.OrcamentoId)
            .HasDatabaseName("ix_orcamento_implante_orcamento");
    }
}
