using Imedto.Backend.Domain.AssinaturaDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AssinaturaCertificadoConfiguration : IEntityTypeConfiguration<AssinaturaCertificado>
{
    public void Configure(EntityTypeBuilder<AssinaturaCertificado> builder)
    {
        builder.ToTable("assinatura_certificados");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.MedicoId).HasColumnName("medico_id").IsRequired();
        // Valores esperados: 'BirdId' | 'VIDaaS'. Sem enum no banco — text controlado pelo domain.
        builder.Property(c => c.Provedor).HasColumnName("provedor").HasColumnType("text").IsRequired();
        // Cifrado com IDataProtectionProvider antes de persistir. Nunca expor em payload.
        builder.Property(c => c.RefreshToken).HasColumnName("refresh_token").HasColumnType("text").IsRequired();
        builder.Property(c => c.ExpiraEm).HasColumnName("expira_em");
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();

        // Um médico só pode ter um certificado por provedor.
        builder.HasIndex(c => new { c.MedicoId, c.Provedor })
            .IsUnique()
            .HasDatabaseName("uq_assinatura_certificados_medico_provedor");

        // Índice simples em medico_id para lookup rápido (CONCURRENTLY criado em arquivo separado).
        builder.HasIndex(c => c.MedicoId)
            .HasDatabaseName("ix_assinatura_certificados_medico");

        builder.Ignore(c => c.DomainEvents);
    }
}

public class AssinaturaAuditLogConfiguration : IEntityTypeConfiguration<AssinaturaAuditLog>
{
    public void Configure(EntityTypeBuilder<AssinaturaAuditLog> builder)
    {
        builder.ToTable("assinatura_audit_log");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        // Sem FK física para receitas — log é permanente mesmo se receita for soft-deleted.
        builder.Property(a => a.ReceitaId).HasColumnName("receita_id").IsRequired();
        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        // Guid do usuário que disparou. Para job de expiração: usar Guid.Empty representando sistema.
        builder.Property(a => a.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(a => a.Acao).HasColumnName("acao").HasColumnType("text").IsRequired();
        builder.Property(a => a.StatusAnterior).HasColumnName("status_anterior").HasColumnType("text");
        builder.Property(a => a.StatusNovo).HasColumnName("status_novo").HasColumnType("text");
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();

        // Lookup por receita (handler de webhook e polling).
        builder.HasIndex(a => a.ReceitaId)
            .HasDatabaseName("ix_assinatura_audit_log_receita");

        // Relatório por estabelecimento ordenado por data.
        builder.HasIndex(a => new { a.EstabelecimentoId, a.CriadoEm })
            .HasDatabaseName("ix_assinatura_audit_log_estab_criado")
            .IsDescending(false, true);
    }
}
