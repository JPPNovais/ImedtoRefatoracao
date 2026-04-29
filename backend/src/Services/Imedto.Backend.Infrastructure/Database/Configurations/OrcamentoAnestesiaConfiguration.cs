using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoAnestesiaConfiguration : IEntityTypeConfiguration<OrcamentoAnestesia>
{
    public void Configure(EntityTypeBuilder<OrcamentoAnestesia> builder)
    {
        builder.ToTable("orcamento_anestesia");
        // 1:1 com Orcamento — OrcamentoId como PK garante anestesia única.
        builder.HasKey(a => a.OrcamentoId);
        builder.Property(a => a.OrcamentoId).HasColumnName("orcamento_id").ValueGeneratedNever();

        builder.Ignore(a => a.Id);
        builder.Ignore(a => a.DomainEvents);

        builder.Property(a => a.TipoAnestesia).HasColumnName("tipo_anestesia")
            .HasMaxLength(20).IsRequired().HasConversion<string>();
        builder.Property(a => a.Valor).HasColumnName("valor")
            .HasPrecision(12, 2).IsRequired();
        builder.Property(a => a.Observacao).HasColumnName("observacao").HasMaxLength(200);
    }
}
