using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cobrancas;

public class EstornoPagamentoConfiguration : IEntityTypeConfiguration<EstornoPagamento>
{
    public void Configure(EntityTypeBuilder<EstornoPagamento> builder)
    {
        builder.ToTable("estorno_pagamentos");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(e => e.PagamentoId).HasColumnName("pagamento_id").IsRequired();
        builder.Property(e => e.CobrancaId).HasColumnName("cobranca_id").IsRequired();
        builder.Property(e => e.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(e => e.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.Motivo).HasColumnName("motivo").IsRequired();
        builder.Property(e => e.LancamentoEstornoId).HasColumnName("lancamento_estorno_id");
        builder.Property(e => e.EstornadoPorUsuarioId).HasColumnName("estornado_por_usuario_id").IsRequired();
        builder.Property(e => e.DataEstorno).HasColumnName("data_estorno").IsRequired();
        builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();

        // unique (pagamento_id) — garante 1 estorno total por pagamento no nível do banco (R8/DC3)
        builder.HasIndex(e => e.PagamentoId)
            .IsUnique()
            .HasDatabaseName("uq_estorno_pagamentos_pagamento_id");

        builder.HasIndex(e => e.CobrancaId)
            .HasDatabaseName("ix_estorno_pagamentos_cobranca_id");
        builder.HasIndex(e => new { e.EstabelecimentoId, e.CobrancaId })
            .HasDatabaseName("ix_estorno_pagamentos_estab_cobranca");

        // FKs ON DELETE RESTRICT — dado financeiro não cascateia (§10 do briefing)
        builder.HasOne<Domain.Cobrancas.Pagamento>()
            .WithMany()
            .HasForeignKey(e => e.PagamentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_estorno_pagamentos_pagamento");

        builder.HasOne<Domain.Cobrancas.Cobranca>()
            .WithMany(c => c.Estornos)
            .HasForeignKey(e => e.CobrancaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_estorno_pagamentos_cobranca");
    }
}
