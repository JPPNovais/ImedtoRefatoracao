using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class CategoriaFinanceiraPadraoSistemaConfiguration : IEntityTypeConfiguration<CategoriaFinanceiraPadraoSistema>
{
    public void Configure(EntityTypeBuilder<CategoriaFinanceiraPadraoSistema> builder)
    {
        builder.ToTable("categorias_financeiras_padrao_sistema");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        builder.Property(c => c.Tipo).HasColumnName("tipo").HasMaxLength(10).IsRequired()
            .HasConversion<string>();
        builder.Property(c => c.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(c => c.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(c => c.AtualizadaEm).HasColumnName("atualizada_em");

        builder.Ignore(c => c.DomainEvents);

        // Chave de negócio do catálogo global: nome + tipo devem ser únicos.
        // (Diferente de categorias_financeiras onde a unicidade é só por nome+estab.)
        builder.HasIndex(c => new { c.Nome, c.Tipo })
            .IsUnique()
            .HasDatabaseName("uq_categorias_financeiras_padrao_sistema_nome_tipo");
    }
}
