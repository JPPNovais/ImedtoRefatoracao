using Imedto.Backend.Domain.Inventario.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cadastros;

public class FabricanteEstoqueConfiguration : IEntityTypeConfiguration<FabricanteEstoque>
{
    public void Configure(EntityTypeBuilder<FabricanteEstoque> builder)
    {
        builder.ToTable("fabricantes_estoque");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(f => f.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(f => f.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(f => f.Pais).HasColumnName("pais").HasMaxLength(60);
        builder.Property(f => f.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(f => f.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(f => f.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(f => new { f.EstabelecimentoId, f.Ativo })
            .HasDatabaseName("ix_fabricantes_estoque_estab_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(f => f.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_fabricantes_estoque_estabelecimento");

        builder.Ignore(f => f.DomainEvents);
    }
}
