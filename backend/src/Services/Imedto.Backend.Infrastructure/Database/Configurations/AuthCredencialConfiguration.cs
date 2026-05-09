using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AuthCredencialConfiguration : IEntityTypeConfiguration<AuthCredencial>
{
    public void Configure(EntityTypeBuilder<AuthCredencial> builder)
    {
        builder.ToTable("auth_credenciais");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        // citext: case-insensitive comparison nativo no Postgres.
        builder.Property(c => c.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.SenhaHash).HasColumnName("senha_hash").HasMaxLength(120);
        builder.Property(c => c.EmailConfirmadoEm).HasColumnName("email_confirmado_em");
        builder.Property(c => c.BloqueadoEm).HasColumnName("bloqueado_em");
        builder.Property(c => c.MotivoBloqueio).HasColumnName("motivo_bloqueio").HasMaxLength(500);
        builder.Property(c => c.TentativasFalhas).HasColumnName("tentativas_falhas").IsRequired().HasDefaultValue(0);
        builder.Property(c => c.UltimoLoginEm).HasColumnName("ultimo_login_em");
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        // Cascata: se a credencial for excluída, o usuário ligado também (LGPD esquecimento).
        // A FK reversa (usuarios.id → auth_credenciais.id) será definida na config de Usuario.
    }
}
