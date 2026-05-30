using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoRegiaoAnatomicaGlobalConfiguration : IEntityTypeConfiguration<ImedtoRegiaoAnatomicaGlobal>
{
    public void Configure(EntityTypeBuilder<ImedtoRegiaoAnatomicaGlobal> builder)
    {
        builder.ToTable("imedto_regiao_anatomica_global");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.Nome).HasColumnName("nome").HasColumnType("text").IsRequired();
        // text[] mapeado via Npgsql native array support.
        builder.Property(r => r.Sinonimos).HasColumnName("sinonimos").HasColumnType("text[]");
        builder.Property(r => r.SistemaCorporal).HasColumnName("sistema_corporal").HasColumnType("text");
        builder.Property(r => r.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(r => r.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(r => r.AtualizadoEm).HasColumnName("atualizado_em");

        // Unique case-insensitive no nome (índice expressional em _indices.sql por ser CONCURRENTLY).
        builder.HasIndex(r => r.Nome)
            .HasDatabaseName("uq_imedto_regiao_anatomica_global_nome_lower")
            .IsUnique();

        // Índice de listagem filtrada (ativo, sistema_corporal, nome).
        builder.HasIndex(r => new { r.Ativo, r.SistemaCorporal, r.Nome })
            .HasDatabaseName("ix_imedto_regiao_anatomica_global_ativo_sistema_nome");

        builder.Ignore(r => r.DomainEvents);
    }
}
