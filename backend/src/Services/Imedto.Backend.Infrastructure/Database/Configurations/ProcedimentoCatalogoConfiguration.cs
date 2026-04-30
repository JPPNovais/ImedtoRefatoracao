using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Catalogo;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProcedimentoCatalogoConfiguration : IEntityTypeConfiguration<ProcedimentoCatalogo>
{
    public void Configure(EntityTypeBuilder<ProcedimentoCatalogo> builder)
    {
        builder.ToTable("catalogo_procedimentos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(p => p.Codigo).HasColumnName("codigo").HasMaxLength(20).IsRequired();
        builder.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(300).IsRequired();
        builder.Property(p => p.Origem)
            .HasColumnName("origem")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(p => p.Capitulo).HasColumnName("capitulo").HasMaxLength(80);
        builder.Property(p => p.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);

        builder.HasIndex(p => p.Codigo).IsUnique().HasDatabaseName("uq_catalogo_procedimentos_codigo");
        builder.HasIndex(p => new { p.Ativo, p.Origem }).HasDatabaseName("ix_catalogo_procedimentos_ativo_origem");

        // TODO: índice GIN/trigram em nome para busca textual eficiente quando pg_trgm estiver habilitada.
        // ALTER TABLE catalogo_procedimentos ADD COLUMN nome_trgm tsvector;
        // CREATE INDEX ix_catalogo_procedimentos_nome_trgm ON catalogo_procedimentos USING GIN (nome gin_trgm_ops);
        // Por ora usamos ILIKE — aceitável para o volume inicial do catálogo.

        builder.Ignore(p => p.DomainEvents);
    }
}
