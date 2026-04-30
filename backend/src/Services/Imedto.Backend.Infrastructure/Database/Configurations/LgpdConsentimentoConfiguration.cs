using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Lgpd;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class LgpdConsentimentoConfiguration : IEntityTypeConfiguration<LgpdConsentimento>
{
    public void Configure(EntityTypeBuilder<LgpdConsentimento> builder)
    {
        builder.ToTable("lgpd_consentimentos");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(c => c.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(c => c.Versao).HasColumnName("versao").IsRequired().HasMaxLength(20);
        builder.Property(c => c.AceitoEm).HasColumnName("aceito_em").IsRequired();
        builder.Property(c => c.IpOrigem).HasColumnName("ip_origem").HasMaxLength(45);
        builder.Property(c => c.UserAgent).HasColumnName("user_agent").HasMaxLength(500);

        // Caso de uso principal: listar consentimentos do titular, ordenado por mais recente.
        builder.HasIndex(c => new { c.UsuarioId, c.Tipo, c.AceitoEm })
            .HasDatabaseName("ix_lgpd_consentimentos_usuario_tipo_data");

        builder.Ignore(c => c.DomainEvents);
    }
}
