using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoInternacaoConfiguration : IEntityTypeConfiguration<OrcamentoInternacao>
{
    public void Configure(EntityTypeBuilder<OrcamentoInternacao> builder)
    {
        builder.ToTable("orcamento_internacao");
        // 1:1 com Orcamento — OrcamentoId é a PK, garantindo no máximo uma internação
        // por orçamento sem precisar de unique index extra.
        builder.HasKey(i => i.OrcamentoId);
        builder.Property(i => i.OrcamentoId).HasColumnName("orcamento_id").ValueGeneratedNever();

        // Id herdado de Entity é ignorado — usamos OrcamentoId como chave de fato.
        builder.Ignore(i => i.Id);
        builder.Ignore(i => i.DomainEvents);

        builder.Property(i => i.TipoInternacao).HasColumnName("tipo_internacao")
            .HasMaxLength(20).IsRequired().HasConversion<string>();
        builder.Property(i => i.Dias).HasColumnName("dias").IsRequired();
        builder.Property(i => i.ValorDiaria).HasColumnName("valor_diaria")
            .HasPrecision(12, 2).IsRequired();
        builder.Property(i => i.ValorTotal).HasColumnName("valor_total")
            .HasPrecision(12, 2).IsRequired();
    }
}
