using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AuthRefreshTokenConfiguration : IEntityTypeConfiguration<AuthRefreshToken>
{
    public void Configure(EntityTypeBuilder<AuthRefreshToken> builder)
    {
        builder.ToTable("auth_refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(t => t.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(t => t.ExpiraEm).HasColumnName("expira_em").IsRequired();
        builder.Property(t => t.RevogadoEm).HasColumnName("revogado_em");
        builder.Property(t => t.IpOrigem).HasColumnName("ip_origem").HasMaxLength(45);
        builder.Property(t => t.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UsuarioId, t.RevogadoEm }).HasDatabaseName("ix_refresh_usuario_ativo");
        builder.HasIndex(t => t.ExpiraEm).HasDatabaseName("ix_refresh_expira");

        builder.HasOne<AuthCredencial>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
