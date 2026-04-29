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

        // Item 2.13: FKs nullable. NÃO declarar HasOne/WithMany para não criar navegação
        // — apenas a coluna SQL. database-architect deve adicionar
        //   FOREIGN KEY (paciente_id)   REFERENCES pacientes(id)              ON DELETE SET NULL
        //   FOREIGN KEY (prontuario_id) REFERENCES prontuarios(id)            ON DELETE SET NULL
        //   FOREIGN KEY (evolucao_id)   REFERENCES prontuario_evolucoes(id)   ON DELETE SET NULL
        // manualmente na migration SQL idempotente da Wave 5.
        builder.Property(a => a.PacienteId).HasColumnName("paciente_id");
        builder.Property(a => a.ProntuarioId).HasColumnName("prontuario_id");
        builder.Property(a => a.EvolucaoId).HasColumnName("evolucao_id");

        builder.HasIndex(a => new { a.UsuarioId, a.CriadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_ai_audit_usuario_data");
        builder.HasIndex(a => new { a.EstabelecimentoId, a.CriadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_ai_audit_estab_data");

        builder.Ignore(a => a.DomainEvents);
    }
}
