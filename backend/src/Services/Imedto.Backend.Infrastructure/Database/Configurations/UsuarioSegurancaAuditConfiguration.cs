using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Auth;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class UsuarioSegurancaAuditConfiguration : IEntityTypeConfiguration<UsuarioSegurancaAudit>
{
    public void Configure(EntityTypeBuilder<UsuarioSegurancaAudit> builder)
    {
        builder.ToTable("usuario_seguranca_audit");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityAlwaysColumn();

        builder.Property(e => e.UsuarioId)
            .HasColumnName("usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(e => e.Acao)
            .HasColumnName("acao")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.OcorridoEm)
            .HasColumnName("ocorrido_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(e => e.IpOrigem)
            .HasColumnName("ip_origem")
            .HasMaxLength(45)
            .HasColumnType("character varying(45)");

        // Índice composto: relatórios de auditoria por usuário ordenados por data.
        builder.HasIndex(e => new { e.UsuarioId, e.OcorridoEm })
            .HasDatabaseName("ix_usuario_seguranca_audit_usuario_data");

        builder.Ignore(e => e.DomainEvents);
    }
}
