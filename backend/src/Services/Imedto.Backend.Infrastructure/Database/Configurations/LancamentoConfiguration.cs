using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(l => l.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(l => l.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(l => l.Descricao).HasColumnName("descricao").HasMaxLength(300).IsRequired();
        builder.Property(l => l.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        builder.Property(l => l.DataVencimento).HasColumnName("data_vencimento").IsRequired();
        builder.Property(l => l.DataPagamento).HasColumnName("data_pagamento");
        builder.Property(l => l.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(l => l.Categoria).HasColumnName("categoria").HasMaxLength(100).IsRequired();
        builder.Property(l => l.OrcamentoId).HasColumnName("orcamento_id");
        // F1: colunas para vínculo com cobrança/pagamento (ALTER TABLE — schema pelo imedto-database)
        builder.Property(l => l.CobrancaId).HasColumnName("cobranca_id");
        builder.Property(l => l.PagamentoId).HasColumnName("pagamento_id");
        builder.Property(l => l.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(l => l.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(l => l.DomainEvents);

        builder.HasIndex(l => new { l.EstabelecimentoId, l.Status, l.DataVencimento })
            .HasDatabaseName("ix_lancamento_estab_status_venc");
        builder.HasIndex(l => new { l.EstabelecimentoId, l.Tipo })
            .HasDatabaseName("ix_lancamento_estab_tipo");
        // F1: índice em cobranca_id para join na query da cobrança sem N+1 (briefing §10)
        builder.HasIndex(l => l.CobrancaId)
            .HasDatabaseName("ix_lancamentos_cobranca_id");
        // F7: agregações de KPI/extrato/caixa filtram por (estabelecimento_id, data_pagamento) — ObterKpis/ListarExtrato/ObterCaixaDiario.
        builder.HasIndex(l => new { l.EstabelecimentoId, l.DataPagamento })
            .HasDatabaseName("ix_lancamentos_estab_data_pagamento");
        // F7: índice parcial para agregação do caixa (status='Pago') — uso frequente nos KPIs de caixa.
        // Nota: HasFilter com valor string é emitido corretamente pelo Npgsql como expressão SQL.
        builder.HasIndex(l => new { l.EstabelecimentoId, l.DataPagamento })
            .HasDatabaseName("ix_lancamentos_estab_data_pagamento_pago")
            .HasFilter("status = 'Pago'");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(l => l.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lancamento_estabelecimento");

        builder.HasOne<Domain.Orcamentos.Orcamento>()
            .WithMany()
            .HasForeignKey(l => l.OrcamentoId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_lancamento_orcamento");
    }
}
