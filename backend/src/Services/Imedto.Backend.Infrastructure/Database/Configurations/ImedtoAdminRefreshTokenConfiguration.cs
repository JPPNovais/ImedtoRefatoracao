using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoAdminRefreshTokenConfiguration : IEntityTypeConfiguration<ImedtoAdminRefreshToken>
{
    public void Configure(EntityTypeBuilder<ImedtoAdminRefreshToken> builder)
    {
        builder.ToTable("imedto_admin_refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.AdminId).HasColumnName("admin_id").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(t => t.ExpiraEm).HasColumnName("expira_em").IsRequired();
        builder.Property(t => t.RevogadoEm).HasColumnName("revogado_em");
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(t => t.IpOrigem).HasColumnName("ip_origem");
        builder.Property(t => t.UserAgent).HasColumnName("user_agent");

        // Índice em admin_id + expira_em: revogação em lote ao desativar admin.
        builder.HasIndex(t => new { t.AdminId, t.ExpiraEm }).HasDatabaseName("ix_imedto_admin_refresh_tokens_admin_expira");

        // Índice único em token_hash: lookup no refresh (parcial WHERE revogado_em IS NULL).
        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("uq_imedto_admin_refresh_tokens_hash");

        // FK para imedto_admins com CASCADE: ao deletar admin, todos os tokens são removidos.
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(t => t.AdminId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.DomainEvents);
    }
}
