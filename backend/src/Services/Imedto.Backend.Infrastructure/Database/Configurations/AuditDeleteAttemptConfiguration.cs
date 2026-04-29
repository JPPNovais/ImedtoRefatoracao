using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Auditoria;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AuditDeleteAttemptConfiguration : IEntityTypeConfiguration<AuditDeleteAttempt>
{
    public void Configure(EntityTypeBuilder<AuditDeleteAttempt> builder)
    {
        builder.ToTable("audit_delete_attempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.Tabela).HasColumnName("tabela").HasMaxLength(80).IsRequired();
        builder.Property(a => a.RegistroId).HasColumnName("registro_id").HasMaxLength(80).IsRequired();
        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(a => a.UsuarioId).HasColumnName("usuario_id");
        builder.Property(a => a.Motivo).HasColumnName("motivo").HasMaxLength(500);
        builder.Property(a => a.TentadoEm).HasColumnName("tentado_em").IsRequired();

        builder.HasIndex(a => new { a.Tabela, a.TentadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_delete_tabela_data");
        builder.HasIndex(a => new { a.EstabelecimentoId, a.TentadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_delete_estab_data");

        builder.Ignore(a => a.DomainEvents);
    }
}
