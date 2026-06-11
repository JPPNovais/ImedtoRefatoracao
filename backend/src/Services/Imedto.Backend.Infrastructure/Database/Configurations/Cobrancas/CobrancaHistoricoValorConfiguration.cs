using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cobrancas;

public class CobrancaHistoricoValorConfiguration : IEntityTypeConfiguration<CobrancaHistoricoValor>
{
    public void Configure(EntityTypeBuilder<CobrancaHistoricoValor> builder)
    {
        builder.ToTable("cobranca_historico_valor");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(h => h.CobrancaId).HasColumnName("cobranca_id").IsRequired();
        builder.Property(h => h.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(h => h.ValorAnterior).HasColumnName("valor_anterior").HasPrecision(14, 2).IsRequired();
        builder.Property(h => h.ValorNovo).HasColumnName("valor_novo").HasPrecision(14, 2).IsRequired();
        builder.Property(h => h.AlteradoPorUsuarioId).HasColumnName("alterado_por_usuario_id").IsRequired();
        builder.Property(h => h.AlteradoEm).HasColumnName("alterado_em").IsRequired();

        builder.HasIndex(h => h.CobrancaId)
            .HasDatabaseName("ix_cobranca_historico_valor_cobranca_id");

        builder.HasOne<Cobranca>()
            .WithMany(c => c.HistoricoValor)
            .HasForeignKey(h => h.CobrancaId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_cobranca_historico_valor_cobranca");
    }
}
