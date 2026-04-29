using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoFormaPagamentoConfiguration : IEntityTypeConfiguration<OrcamentoFormaPagamento>
{
    public void Configure(EntityTypeBuilder<OrcamentoFormaPagamento> builder)
    {
        builder.ToTable("orcamento_formas_pagamento");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(f => f.OrcamentoId).HasColumnName("orcamento_id").IsRequired();
        builder.Property(f => f.FormaPagamentoId).HasColumnName("forma_pagamento_id").IsRequired();
        builder.Property(f => f.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        builder.Property(f => f.Parcelas).HasColumnName("parcelas").IsRequired();
        // Item 7 — campos de paridade com o legado: juros/entrada por forma de pagamento.
        // Default 0 preserva orçamentos antigos que só usavam valor/parcelas/observação.
        builder.Property(f => f.AcrescimoPercentual).HasColumnName("acrescimo_percentual")
            .HasPrecision(5, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(f => f.EntradaPercentual).HasColumnName("entrada_percentual")
            .HasPrecision(5, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(f => f.Observacao).HasColumnName("observacao").HasMaxLength(200);
        builder.Property(f => f.Ordem).HasColumnName("ordem").IsRequired();

        builder.Ignore(f => f.DomainEvents);

        builder.HasOne<Domain.Financeiro.FormaPagamento>()
            .WithMany()
            .HasForeignKey(f => f.FormaPagamentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_orcamento_forma_pagamento_forma");

        builder.HasIndex(f => f.OrcamentoId)
            .HasDatabaseName("ix_orcamento_forma_pagamento_orcamento");
    }
}
