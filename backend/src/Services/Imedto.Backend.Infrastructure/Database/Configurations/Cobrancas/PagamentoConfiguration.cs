using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cobrancas;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("pagamentos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(p => p.CobrancaId).HasColumnName("cobranca_id").IsRequired();
        builder.Property(p => p.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.FormaPagamentoId).HasColumnName("forma_pagamento_id").IsRequired();
        builder.Property(p => p.Parcelas).HasColumnName("parcelas").IsRequired().HasDefaultValue(1);
        builder.Property(p => p.Juros).HasColumnName("juros").HasPrecision(12, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.Taxa).HasColumnName("taxa").HasPrecision(12, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.DataPagamento).HasColumnName("data_pagamento").IsRequired();
        builder.Property(p => p.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id").IsRequired();
        builder.Property(p => p.LancamentoId).HasColumnName("lancamento_id").IsRequired(false);
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => p.CobrancaId)
            .HasDatabaseName("ix_pagamentos_cobranca_id");
        builder.HasIndex(p => p.FormaPagamentoId)
            .HasDatabaseName("ix_pagamentos_forma_pagamento_id");

        builder.HasOne<Domain.Financeiro.FormaPagamento>()
            .WithMany()
            .HasForeignKey(p => p.FormaPagamentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pagamentos_forma_pagamento");
    }
}
