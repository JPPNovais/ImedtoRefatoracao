using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Profissionais;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> builder)
    {
        builder.ToTable("profissionais");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("usuario_id");

        builder.Property(p => p.Conselho).HasColumnName("conselho").IsRequired().HasMaxLength(20);
        builder.Property(p => p.Uf).HasColumnName("uf").IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(p => p.NumeroRegistro).HasColumnName("numero_registro").IsRequired().HasMaxLength(30);
        builder.Property(p => p.Especialidade).HasColumnName("especialidade").HasMaxLength(200);
        builder.Property(p => p.Bio).HasColumnName("bio").HasMaxLength(2000);
        builder.Property(p => p.FotoUrl).HasColumnName("foto_url").HasMaxLength(2000);
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(p => p.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        builder.HasIndex(p => new { p.Conselho, p.Uf, p.NumeroRegistro })
            .IsUnique()
            .HasDatabaseName("uq_profissionais_conselho_uf_numero");

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.EstaDeletado);
    }
}
