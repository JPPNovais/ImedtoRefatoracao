using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Salas;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class TipoSalaConfiguration : IEntityTypeConfiguration<TipoSala>
{
    public void Configure(EntityTypeBuilder<TipoSala> builder)
    {
        builder.ToTable("tipo_sala_atendimento");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(t => t.Nome).HasColumnName("nome").IsRequired().HasMaxLength(100);
        builder.Property(t => t.Descricao).HasColumnName("descricao").HasMaxLength(500);

        builder.HasIndex(t => t.Nome).IsUnique().HasDatabaseName("uq_tipo_sala_nome");

        builder.Ignore(t => t.DomainEvents);
    }
}
