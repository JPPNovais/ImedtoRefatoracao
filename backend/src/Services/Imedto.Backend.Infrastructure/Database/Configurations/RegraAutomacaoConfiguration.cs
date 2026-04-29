using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class RegraAutomacaoConfiguration : IEntityTypeConfiguration<RegraAutomacao>
{
    public void Configure(EntityTypeBuilder<RegraAutomacao> builder)
    {
        builder.ToTable("automation_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(r => r.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(r => r.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        builder.Property(r => r.EventoGatilho).HasColumnName("evento_gatilho").HasMaxLength(60).IsRequired();
        builder.Property(r => r.CondicoesJson).HasColumnName("condicoes_json").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.AcoesJson).HasColumnName("acoes_json").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.Ativa).HasColumnName("ativa").IsRequired();
        builder.Property(r => r.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(r => r.AtualizadoEm).HasColumnName("atualizado_em");

        // Caminho quente: dispatch de evento → busca regras ativas por (estab, evento).
        builder.HasIndex(r => new { r.EstabelecimentoId, r.EventoGatilho, r.Ativa })
            .HasDatabaseName("ix_automation_rules_estab_evento_ativa");

        builder.Ignore(r => r.DomainEvents);
    }
}
