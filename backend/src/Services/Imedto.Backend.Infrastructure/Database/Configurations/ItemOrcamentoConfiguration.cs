using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ItemOrcamentoConfiguration : IEntityTypeConfiguration<ItemOrcamento>
{
    public void Configure(EntityTypeBuilder<ItemOrcamento> builder)
    {
        builder.ToTable("itens_orcamento");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(i => i.OrcamentoId).HasColumnName("orcamento_id").IsRequired();
        builder.Property(i => i.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasPrecision(10, 3).IsRequired();
        builder.Property(i => i.ValorUnitario).HasColumnName("valor_unitario").HasPrecision(12, 2).IsRequired();
        builder.Property(i => i.DescontoPercent).HasColumnName("desconto_percent").HasPrecision(5, 2).IsRequired();
        builder.Property(i => i.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2).IsRequired();

        builder.Ignore(i => i.DomainEvents);

        builder.HasIndex(i => i.OrcamentoId)
            .HasDatabaseName("ix_item_orcamento_orcamento");
    }
}
