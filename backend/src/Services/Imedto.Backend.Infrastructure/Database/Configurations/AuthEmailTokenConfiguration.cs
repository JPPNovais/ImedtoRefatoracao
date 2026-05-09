using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AuthEmailTokenConfiguration : IEntityTypeConfiguration<AuthEmailToken>
{
    public void Configure(EntityTypeBuilder<AuthEmailToken> builder)
    {
        builder.ToTable("auth_email_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(t => t.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(t => t.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(t => t.ExpiraEm).HasColumnName("expira_em").IsRequired();
        builder.Property(t => t.ConsumidoEm).HasColumnName("consumido_em");
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UsuarioId, t.Tipo }).HasDatabaseName("ix_email_token_usuario_tipo");

        builder.HasOne<AuthCredencial>()
            .WithMany()
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
