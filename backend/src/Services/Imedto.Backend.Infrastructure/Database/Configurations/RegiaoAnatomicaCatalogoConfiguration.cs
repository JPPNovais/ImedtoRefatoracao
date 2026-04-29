using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Catalogo;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class RegiaoAnatomicaCatalogoConfiguration : IEntityTypeConfiguration<RegiaoAnatomicaCatalogo>
{
    public void Configure(EntityTypeBuilder<RegiaoAnatomicaCatalogo> builder)
    {
        builder.ToTable("regioes_anatomicas_catalogo");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(r => r.Codigo).HasColumnName("codigo").HasMaxLength(60).IsRequired();
        builder.Property(r => r.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        builder.Property(r => r.PaiCodigo).HasColumnName("pai_codigo").HasMaxLength(60);
        builder.Property(r => r.Nivel).HasColumnName("nivel").IsRequired();
        builder.Property(r => r.Vista).HasColumnName("vista").HasMaxLength(20);
        builder.Property(r => r.TemplateTexto).HasColumnName("template_texto");
        builder.Property(r => r.SvgCoordsJson).HasColumnName("svg_coords").HasColumnType("jsonb");
        builder.Property(r => r.Ordem).HasColumnName("ordem").IsRequired().HasDefaultValue((short)0);
        builder.Property(r => r.Lateralidade).HasColumnName("lateralidade").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);

        builder.HasIndex(r => r.Codigo).IsUnique().HasDatabaseName("uq_regioes_anatomicas_catalogo_codigo");
        builder.HasIndex(r => r.Vista).HasDatabaseName("ix_regioes_anatomicas_catalogo_vista");
        builder.HasIndex(r => new { r.Ativo, r.Vista }).HasDatabaseName("ix_regioes_anatomicas_catalogo_ativo_vista");

        builder.Ignore(r => r.DomainEvents);
    }
}
