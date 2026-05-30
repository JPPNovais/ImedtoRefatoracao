using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoAdminAuditLogConfiguration : IEntityTypeConfiguration<ImedtoAdminAuditLog>
{
    public void Configure(EntityTypeBuilder<ImedtoAdminAuditLog> builder)
    {
        builder.ToTable("imedto_admin_audit_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(l => l.AdminId).HasColumnName("admin_id");
        builder.Property(l => l.Acao).HasColumnName("acao").IsRequired();
        builder.Property(l => l.RecursoTipo).HasColumnName("recurso_tipo");
        builder.Property(l => l.RecursoId).HasColumnName("recurso_id");
        builder.Property(l => l.TenantAfetadoId).HasColumnName("tenant_afetado_id");
        builder.Property(l => l.Motivo).HasColumnName("motivo");
        builder.Property(l => l.Ip).HasColumnName("ip");
        builder.Property(l => l.UserAgent).HasColumnName("user_agent");
        builder.Property(l => l.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();

        // Índice primário de consulta temporal: "últimas ações do sistema".
        builder.HasIndex(l => l.CriadoEm).HasDatabaseName("ix_imedto_admin_audit_log_criado_em");

        // Índice por admin: "o que esse admin fez?".
        builder.HasIndex(l => new { l.AdminId, l.CriadoEm }).HasDatabaseName("ix_imedto_admin_audit_log_admin_criado");

        // Índice por tenant afetado: "quem mexeu nesse tenant?".
        builder.HasIndex(l => new { l.TenantAfetadoId, l.CriadoEm })
            .HasFilter("tenant_afetado_id IS NOT NULL")
            .HasDatabaseName("ix_imedto_admin_audit_log_tenant_criado");

        // Índice por ação + data: "quantos resets ocorreram hoje?".
        builder.HasIndex(l => new { l.Acao, l.CriadoEm }).HasDatabaseName("ix_imedto_admin_audit_log_acao_criado");

        // FK para imedto_admins com SET NULL: log permanece mesmo se admin for excluído.
        // Admin jamais é deletado fisicamente (apenas desativado), mas FK com SET NULL protege integridade.
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(l => l.AdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Sem FK para estabelecimentos.id pois a referência é fraca (log não deve bloquear delete de tenant).
        // tenant_afetado_id é registrado como dado histórico; se o tenant for deletado, o audit permanece.

        builder.Ignore(l => l.DomainEvents);
    }
}
