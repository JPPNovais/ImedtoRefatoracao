using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Usuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(320);
        builder.Property(u => u.NomeCompleto).HasColumnName("nome_completo").HasMaxLength(200);
        builder.Property(u => u.Cpf).HasColumnName("cpf").HasMaxLength(11);
        builder.Property(u => u.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(u => u.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.OnboardingCompleto).HasColumnName("onboarding_completo").IsRequired();
        builder.Property(u => u.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(u => u.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(u => u.UltimoAcessoEm).HasColumnName("ultimo_acesso_em");

        builder.HasIndex(u => u.Email).HasDatabaseName("ix_usuarios_email");
        builder.HasIndex(u => u.Cpf).IsUnique().HasDatabaseName("uq_usuarios_cpf")
            .HasFilter("cpf IS NOT NULL");

        builder.Ignore(u => u.DomainEvents);
    }
}
