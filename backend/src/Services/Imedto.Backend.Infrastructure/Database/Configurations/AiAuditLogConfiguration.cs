using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AiAuditLogConfiguration : IEntityTypeConfiguration<AiAuditLog>
{
    public void Configure(EntityTypeBuilder<AiAuditLog> builder)
    {
        builder.ToTable("ai_audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.PromptHash).HasColumnName("prompt_hash").HasMaxLength(64).IsRequired();
        builder.Property(a => a.ResponseHash).HasColumnName("response_hash").HasMaxLength(64);
        builder.Property(a => a.TokensIn).HasColumnName("tokens_in");
        builder.Property(a => a.TokensOut).HasColumnName("tokens_out");
        builder.Property(a => a.Modelo).HasColumnName("modelo").HasMaxLength(80).IsRequired();
        builder.Property(a => a.Endpoint).HasColumnName("endpoint").HasMaxLength(80).IsRequired();
        builder.Property(a => a.DuracaoMs).HasColumnName("duracao_ms");
        builder.Property(a => a.Sucesso).HasColumnName("sucesso").IsRequired();
        builder.Property(a => a.ErroMensagem).HasColumnName("erro_mensagem").HasMaxLength(500);
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(a => new { a.UsuarioId, a.CriadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_ai_audit_usuario_data");
        builder.HasIndex(a => new { a.EstabelecimentoId, a.CriadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_ai_audit_estab_data");

        builder.Ignore(a => a.DomainEvents);
    }
}
