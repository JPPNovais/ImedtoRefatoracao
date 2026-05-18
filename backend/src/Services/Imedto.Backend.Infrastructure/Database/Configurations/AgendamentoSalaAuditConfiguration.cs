using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Agendamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AgendamentoSalaAuditConfiguration : IEntityTypeConfiguration<AgendamentoSalaAudit>
{
    public void Configure(EntityTypeBuilder<AgendamentoSalaAudit> builder)
    {
        builder.ToTable("agendamento_sala_audit");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.AgendamentoId).HasColumnName("agendamento_id").IsRequired();
        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.SalaIdAnterior).HasColumnName("sala_id_anterior");
        builder.Property(a => a.SalaIdNova).HasColumnName("sala_id_nova");
        builder.Property(a => a.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(a => a.Em).HasColumnName("em").IsRequired();

        builder.HasIndex(a => a.AgendamentoId).HasDatabaseName("ix_agendamento_sala_audit_agendamento");
        builder.HasIndex(a => a.EstabelecimentoId).HasDatabaseName("ix_agendamento_sala_audit_estab");

        builder.Ignore(a => a.DomainEvents);
    }
}
