using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Inventario;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ItemInventarioConfiguration : IEntityTypeConfiguration<ItemInventario>
{
    public void Configure(EntityTypeBuilder<ItemInventario> builder)
    {
        builder.ToTable("itens_inventario");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(i => i.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(i => i.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
        builder.Property(i => i.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Categoria).HasColumnName("categoria").HasMaxLength(100).IsRequired();
        builder.Property(i => i.UnidadeMedida).HasColumnName("unidade_medida").HasMaxLength(30).IsRequired();
        builder.Property(i => i.QuantidadeAtual).HasColumnName("quantidade_atual").HasPrecision(12, 3).IsRequired();
        builder.Property(i => i.QuantidadeMinima).HasColumnName("quantidade_minima").HasPrecision(12, 3).IsRequired();
        builder.Property(i => i.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(i => i.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(i => new { i.EstabelecimentoId, i.Codigo }).IsUnique()
            .HasDatabaseName("uq_inventario_codigo_por_estab");
        builder.HasIndex(i => new { i.EstabelecimentoId, i.Ativo })
            .HasDatabaseName("ix_inventario_estab_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(i => i.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventario_estabelecimento");

        builder.Ignore(i => i.DomainEvents);
        builder.Ignore(i => i.EstoqueAbaixoDoMinimo);
    }
}
