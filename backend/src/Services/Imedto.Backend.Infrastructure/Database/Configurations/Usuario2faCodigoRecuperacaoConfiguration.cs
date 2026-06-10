using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class Usuario2faCodigoRecuperacaoConfiguration : IEntityTypeConfiguration<Usuario2faCodigoRecuperacao>
{
    public void Configure(EntityTypeBuilder<Usuario2faCodigoRecuperacao> builder)
    {
        builder.ToTable("usuario_2fa_codigo_recuperacao");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityAlwaysColumn();

        builder.Property(e => e.UsuarioId)
            .HasColumnName("usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.CodigoHash)
            .HasColumnName("codigo_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.UsadoEm)
            .HasColumnName("usado_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // JaUsado é propriedade calculada — não é coluna.
        builder.Ignore(e => e.JaUsado);

        // Índice principal: buscar códigos disponíveis de um usuário no passo 2 do login.
        builder.HasIndex(e => e.UsuarioId)
            .HasDatabaseName("ix_usuario_2fa_codigo_recuperacao_usuario");

        builder.Ignore(e => e.DomainEvents);
    }
}
