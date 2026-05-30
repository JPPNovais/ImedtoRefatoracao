using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoAdminConfiguration : IEntityTypeConfiguration<ImedtoAdmin>
{
    public void Configure(EntityTypeBuilder<ImedtoAdmin> builder)
    {
        builder.ToTable("imedto_admins");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        // citext: comparação case-insensitive nativa no Postgres — e-mail único sem sensibilidade a maiúsculas.
        builder.Property(a => a.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
        builder.Property(a => a.Nome).HasColumnName("nome").IsRequired();
        builder.Property(a => a.SenhaHash).HasColumnName("senha_hash").IsRequired();
        builder.Property(a => a.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(a => a.ForcePasswordReset).HasColumnName("force_password_reset").HasDefaultValue(false).IsRequired();
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(a => a.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(a => a.UltimoLoginEm).HasColumnName("ultimo_login_em");
        builder.Property(a => a.CriadoPorAdminId).HasColumnName("criado_por_admin_id");
        builder.Property(a => a.DesativadoEm).HasColumnName("desativado_em");
        builder.Property(a => a.DesativadoPorAdminId).HasColumnName("desativado_por_admin_id");

        // Índice único em email (citext já faz comparação case-insensitive no Postgres).
        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("uq_imedto_admins_email");

        // Índice em ativo: listagem de admins sempre filtra por ativo.
        builder.HasIndex(a => a.Ativo).HasDatabaseName("ix_imedto_admins_ativo");

        // FK self-referencial para criador (nullable — seed inicial não tem criador).
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(a => a.CriadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK self-referencial para desativador (nullable).
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(a => a.DesativadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(a => a.DomainEvents);
    }
}
