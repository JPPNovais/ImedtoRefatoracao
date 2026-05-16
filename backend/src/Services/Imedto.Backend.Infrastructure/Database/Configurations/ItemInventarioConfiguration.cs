using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;

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
        // Coluna legada — mantida durante deprecation. Sincronizada pelo aggregate
        // a partir do snapshot da CategoriaEstoque.Nome.
        builder.Property(i => i.Categoria).HasColumnName("categoria").HasMaxLength(100).IsRequired();
        builder.Property(i => i.CategoriaId).HasColumnName("categoria_id").IsRequired();
        builder.Property(i => i.FabricanteId).HasColumnName("fabricante_id");
        builder.Property(i => i.FornecedorPadraoId).HasColumnName("fornecedor_padrao_id");
        builder.Property(i => i.LocalPadraoId).HasColumnName("local_padrao_id");
        builder.Property(i => i.UnidadeMedida).HasColumnName("unidade_medida").HasMaxLength(30).IsRequired();
        builder.Property(i => i.QuantidadeAtual).HasColumnName("quantidade_atual").HasPrecision(12, 3).IsRequired();
        builder.Property(i => i.QuantidadeMinima).HasColumnName("quantidade_minima").HasPrecision(12, 3).IsRequired();
        builder.Property(i => i.CustoMedio).HasColumnName("custo_medio").HasPrecision(18, 4).IsRequired();
        builder.Property(i => i.CustoUnitario).HasColumnName("custo_unitario").HasPrecision(12, 2);
        builder.Property(i => i.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(i => i.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(i => new { i.EstabelecimentoId, i.Codigo }).IsUnique()
            .HasDatabaseName("uq_inventario_codigo_por_estab");
        builder.HasIndex(i => new { i.EstabelecimentoId, i.Ativo })
            .HasDatabaseName("ix_inventario_estab_ativo");
        builder.HasIndex(i => new { i.EstabelecimentoId, i.CategoriaId })
            .HasDatabaseName("ix_inventario_estab_categoria");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(i => i.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventario_estabelecimento");

        builder.HasOne<CategoriaEstoque>()
            .WithMany()
            .HasForeignKey(i => i.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventario_categoria");

        builder.HasOne<FabricanteEstoque>()
            .WithMany()
            .HasForeignKey(i => i.FabricanteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventario_fabricante");

        builder.HasOne<FornecedorEstoque>()
            .WithMany()
            .HasForeignKey(i => i.FornecedorPadraoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventario_fornecedor_padrao");

        builder.HasOne<LocalEstoque>()
            .WithMany()
            .HasForeignKey(i => i.LocalPadraoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_inventario_local_padrao");

        builder.Ignore(i => i.DomainEvents);
        builder.Ignore(i => i.EstoqueAbaixoDoMinimo);
    }
}
