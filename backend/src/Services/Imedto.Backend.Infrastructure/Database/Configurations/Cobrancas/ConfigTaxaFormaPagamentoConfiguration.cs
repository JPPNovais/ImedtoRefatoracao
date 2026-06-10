using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cobrancas;

public class ConfigTaxaFormaPagamentoConfiguration : IEntityTypeConfiguration<ConfigTaxaFormaPagamento>
{
    public void Configure(EntityTypeBuilder<ConfigTaxaFormaPagamento> builder)
    {
        builder.ToTable("config_taxa_forma_pagamento");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.FormaPagamentoId).HasColumnName("forma_pagamento_id").IsRequired();
        // numeric(6,3) para taxa percentual com 3 decimais
        builder.Property(c => c.TaxaPercentual).HasColumnName("taxa_percentual").HasPrecision(6, 3).IsRequired()
            .HasDefaultValue(0m);
        builder.Property(c => c.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(c => c.DomainEvents);

        // Índice na FK de forma_pagamento_id (Postgres não cria automaticamente)
        builder.HasIndex(c => c.FormaPagamentoId)
            .HasDatabaseName("ix_config_taxa_forma_pagamento_forma_id");
        // Único por estabelecimento + forma de pagamento
        builder.HasIndex(c => new { c.EstabelecimentoId, c.FormaPagamentoId })
            .IsUnique()
            .HasDatabaseName("uq_config_taxa_forma_pagamento_estab_forma");

        builder.HasOne<Domain.Financeiro.FormaPagamento>()
            .WithMany()
            .HasForeignKey(c => c.FormaPagamentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_config_taxa_forma_pagamento_forma");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_config_taxa_forma_pagamento_estabelecimento");
    }
}
