using Imedto.Backend.Domain.Inventario.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cadastros;

public class CategoriaEstoqueConfiguration : IEntityTypeConfiguration<CategoriaEstoque>
{
    public void Configure(EntityTypeBuilder<CategoriaEstoque> builder)
    {
        builder.ToTable("categorias_estoque");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        builder.Property(c => c.Cor).HasColumnName("cor").HasMaxLength(40).IsRequired();
        builder.Property(c => c.Icone).HasColumnName("icone").HasMaxLength(40).IsRequired();
        builder.Property(c => c.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        // Unicidade case-insensitive feita no nível SQL (índice em lower(nome)).
        // Index expression precisa ser SQL no migration custom — aqui ficamos só com o B-tree de filtro.
        builder.HasIndex(c => new { c.EstabelecimentoId, c.Ativo })
            .HasDatabaseName("ix_categorias_estoque_estab_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_categorias_estoque_estabelecimento");

        builder.Ignore(c => c.DomainEvents);
    }
}
