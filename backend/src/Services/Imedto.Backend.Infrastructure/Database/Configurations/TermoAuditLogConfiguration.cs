using Imedto.Backend.Domain.Termos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class TermoAuditLogConfiguration : IEntityTypeConfiguration<TermoAuditLog>
{
    public void Configure(EntityTypeBuilder<TermoAuditLog> builder)
    {
        builder.ToTable("termo_audit_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(l => l.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(l => l.UsuarioId).HasColumnName("usuario_id");
        builder.Property(l => l.Acao).HasColumnName("acao").HasMaxLength(TermoAuditLog.AcaoMaximo).IsRequired();
        builder.Property(l => l.Entidade).HasColumnName("entidade").HasMaxLength(TermoAuditLog.EntidadeMaximo).IsRequired();
        builder.Property(l => l.EntidadeId).HasColumnName("entidade_id").IsRequired();
        builder.Property(l => l.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        builder.Property(l => l.IpOrigem).HasColumnName("ip_origem").HasMaxLength(45);
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(l => new { l.EstabelecimentoId, l.CriadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_termo_audit_log_estab_criado");

        builder.HasIndex(l => new { l.Entidade, l.EntidadeId, l.CriadoEm })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_termo_audit_log_entidade_criado");

        builder.Ignore(l => l.DomainEvents);
    }
}
