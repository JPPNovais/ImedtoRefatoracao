using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Lgpd;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class LgpdAnonimizacaoConfiguration : IEntityTypeConfiguration<LgpdAnonimizacao>
{
    public void Configure(EntityTypeBuilder<LgpdAnonimizacao> builder)
    {
        builder.ToTable("lgpd_anonimizacoes");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.Tabela).HasColumnName("tabela").IsRequired().HasMaxLength(80);
        builder.Property(a => a.RegistroId).HasColumnName("registro_id").IsRequired();
        builder.Property(a => a.Motivo).HasColumnName("motivo").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(a => a.AnonimizadoEm).HasColumnName("anonimizado_em").IsRequired();
        builder.Property(a => a.ExecutadoPorUsuarioId).HasColumnName("executado_por_usuario_id");

        // Busca por registro específico (verificar histórico de um paciente).
        builder.HasIndex(a => new { a.Tabela, a.RegistroId })
            .HasDatabaseName("ix_lgpd_anonimizacoes_tabela_registro");

        // Relatório por motivo e data (auditoria periódica).
        builder.HasIndex(a => new { a.Motivo, a.AnonimizadoEm })
            .HasDatabaseName("ix_lgpd_anonimizacoes_motivo_data");

        builder.Ignore(a => a.DomainEvents);
    }
}
