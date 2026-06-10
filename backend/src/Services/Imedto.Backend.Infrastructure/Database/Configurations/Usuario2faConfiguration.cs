using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class Usuario2faConfiguration : IEntityTypeConfiguration<Usuario2fa>
{
    public void Configure(EntityTypeBuilder<Usuario2fa> builder)
    {
        builder.ToTable("usuario_2fa");

        // PK = usuario_id (relação 1:1 — sem IDENTITY, valor fornecido pelo domínio).
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("usuario_id").ValueGeneratedNever();

        // usuario_id como coluna redundante mapeada para permitir query por FK sem shadow property.
        // EF Core usa a coluna "id" (PK) — ignoramos a propriedade UsuarioId para evitar coluna duplicada.
        builder.Ignore(e => e.UsuarioId);

        builder.Property(e => e.SegredoCifrado)
            .HasColumnName("segredo_cifrado")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.AtivadoEm)
            .HasColumnName("ativado_em")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone");

        // Ativo é propriedade calculada — não é coluna.
        builder.Ignore(e => e.Ativo);

        builder.Ignore(e => e.DomainEvents);
    }
}
