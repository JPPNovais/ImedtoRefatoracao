using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class CategoriaFinanceiraConfiguration : IEntityTypeConfiguration<CategoriaFinanceira>
{
    public void Configure(EntityTypeBuilder<CategoriaFinanceira> builder)
    {
        builder.ToTable("categorias_financeiras");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        builder.Property(c => c.Tipo).HasColumnName("tipo").HasMaxLength(10).IsRequired()
            .HasConversion<string>();
        builder.Property(c => c.Padrao).HasColumnName("padrao").IsRequired();
        builder.Property(c => c.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(c => c.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(c => c.AtualizadaEm).HasColumnName("atualizada_em");

        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => new { c.EstabelecimentoId, c.Nome })
            .IsUnique()
            .HasDatabaseName("uq_categoria_financeira_estab_nome");
        builder.HasIndex(c => new { c.EstabelecimentoId, c.Tipo, c.Ativo })
            .HasDatabaseName("ix_categoria_financeira_estab_tipo_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_categoria_financeira_estabelecimento");
    }
}
