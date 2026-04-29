using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class FormaPagamentoConfiguration : IEntityTypeConfiguration<FormaPagamento>
{
    public void Configure(EntityTypeBuilder<FormaPagamento> builder)
    {
        builder.ToTable("formas_pagamento");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(f => f.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(f => f.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        builder.Property(f => f.Padrao).HasColumnName("padrao").IsRequired();
        builder.Property(f => f.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(f => f.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(f => f.AtualizadaEm).HasColumnName("atualizada_em");

        builder.Ignore(f => f.DomainEvents);

        builder.HasIndex(f => new { f.EstabelecimentoId, f.Nome })
            .IsUnique()
            .HasDatabaseName("uq_forma_pagamento_estab_nome");
        builder.HasIndex(f => new { f.EstabelecimentoId, f.Ativo })
            .HasDatabaseName("ix_forma_pagamento_estab_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(f => f.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_forma_pagamento_estabelecimento");
    }
}
