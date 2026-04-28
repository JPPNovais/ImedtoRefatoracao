using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProntuarioAcessoLogConfiguration : IEntityTypeConfiguration<ProntuarioAcessoLog>
{
    public void Configure(EntityTypeBuilder<ProntuarioAcessoLog> builder)
    {
        builder.ToTable("prontuario_acesso_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(l => l.ProntuarioId).HasColumnName("prontuario_id").IsRequired();
        builder.Property(l => l.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(l => l.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(l => l.TipoAcesso).HasColumnName("tipo_acesso").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.OcorridoEm).HasColumnName("ocorrido_em").IsRequired();

        builder.HasIndex(l => new { l.ProntuarioId, l.OcorridoEm }).HasDatabaseName("ix_acesso_log_prontuario_data");
        builder.HasIndex(l => l.UsuarioId).HasDatabaseName("ix_acesso_log_usuario");

        builder.Ignore(l => l.DomainEvents);
    }
}
