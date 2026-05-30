using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoVariavelPoolGlobalConfiguration : IEntityTypeConfiguration<ImedtoVariavelPoolGlobal>
{
    public void Configure(EntityTypeBuilder<ImedtoVariavelPoolGlobal> builder)
    {
        builder.ToTable("imedto_variavel_pool_global");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(v => v.Nome).HasColumnName("nome").HasColumnType("text").IsRequired();
        builder.Property(v => v.Tipo).HasColumnName("tipo").HasColumnType("text").IsRequired();
        builder.Property(v => v.ValoresJson)
            .HasColumnName("valores_json")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");
        builder.Property(v => v.Descricao).HasColumnName("descricao").HasColumnType("text");
        builder.Property(v => v.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(v => v.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(v => v.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(v => v.CriadoPorAdminId).HasColumnName("criado_por_admin_id");
        builder.Property(v => v.AtualizadoPorAdminId).HasColumnName("atualizado_por_admin_id");

        // FKs para admin — nullable, SET NULL ao excluir admin.
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(v => v.CriadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(v => v.AtualizadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique case-insensitive no nome (índice expressional em _indices.sql por ser CONCURRENTLY).
        builder.HasIndex(v => v.Nome)
            .HasDatabaseName("uq_imedto_variavel_pool_global_nome_lower")
            .IsUnique();

        // Índice de listagem filtrada (ativo, tipo, nome).
        builder.HasIndex(v => new { v.Ativo, v.Tipo, v.Nome })
            .HasDatabaseName("ix_imedto_variavel_pool_global_ativo_tipo_nome");

        builder.Ignore(v => v.DomainEvents);
    }
}
