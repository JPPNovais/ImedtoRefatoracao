using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Pacientes;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class PacienteAcessoLogConfiguration : IEntityTypeConfiguration<PacienteAcessoLog>
{
    public void Configure(EntityTypeBuilder<PacienteAcessoLog> builder)
    {
        builder.ToTable("paciente_acesso_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(l => l.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(l => l.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(l => l.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(l => l.TipoAcesso).HasColumnName("tipo_acesso").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.OcorridoEm).HasColumnName("ocorrido_em").IsRequired();
        builder.Property(l => l.IpOrigem).HasColumnName("ip_origem").HasMaxLength(45); // 45 = IPv6 max

        builder.HasIndex(l => new { l.PacienteId, l.OcorridoEm }).HasDatabaseName("ix_paciente_acesso_log_paciente_data");
        builder.HasIndex(l => l.UsuarioId).HasDatabaseName("ix_paciente_acesso_log_usuario");
        builder.HasIndex(l => new { l.EstabelecimentoId, l.OcorridoEm }).HasDatabaseName("ix_paciente_acesso_log_estab_data");

        builder.Ignore(l => l.DomainEvents);
    }
}
