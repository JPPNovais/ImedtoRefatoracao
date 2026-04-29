using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Inventario;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class MovimentacaoEstoqueConfiguration : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> builder)
    {
        builder.ToTable("movimentacoes_estoque");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(m => m.ItemInventarioId).HasColumnName("item_inventario_id").IsRequired();
        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(m => m.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(m => m.Quantidade).HasColumnName("quantidade").HasPrecision(12, 3).IsRequired();
        builder.Property(m => m.QuantidadeAnterior).HasColumnName("quantidade_anterior").HasPrecision(12, 3).IsRequired();
        builder.Property(m => m.QuantidadeApos).HasColumnName("quantidade_apos").HasPrecision(12, 3).IsRequired();
        builder.Property(m => m.CustoUnitario).HasColumnName("custo_unitario").HasPrecision(18, 4).IsRequired();
        builder.Property(m => m.CustoTotal).HasColumnName("custo_total").HasPrecision(18, 4).IsRequired();
        builder.Property(m => m.Observacao).HasColumnName("observacao").HasMaxLength(500);
        builder.Property(m => m.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(m => m.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        builder.HasIndex(m => new { m.ItemInventarioId, m.CriadoEm })
            .HasDatabaseName("ix_movimentacao_item_data");
        builder.HasIndex(m => new { m.EstabelecimentoId, m.CriadoEm })
            .HasDatabaseName("ix_movimentacao_estab_data");

        builder.HasOne<ItemInventario>()
            .WithMany()
            .HasForeignKey(m => m.ItemInventarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimentacao_item_inventario");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(m => m.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimentacao_estabelecimento");

        builder.Ignore(m => m.DomainEvents);
    }
}
