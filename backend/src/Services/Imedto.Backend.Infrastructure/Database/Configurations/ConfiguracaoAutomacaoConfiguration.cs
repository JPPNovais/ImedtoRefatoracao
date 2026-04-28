using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ConfiguracaoAutomacaoConfiguration : IEntityTypeConfiguration<ConfiguracaoAutomacao>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoAutomacao> builder)
    {
        builder.ToTable("configuracoes_automacao");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.LembretesHabilitados).HasColumnName("lembretes_habilitados").IsRequired();
        builder.Property(c => c.HorasAntecedenciaLembrete).HasColumnName("horas_antecedencia_lembrete").IsRequired();
        builder.Property(c => c.ExpiracaoOrcamentosHabilitada).HasColumnName("expiracao_orcamentos_habilitada").IsRequired();
        builder.Property(c => c.EmailRemetente).HasColumnName("email_remetente").HasMaxLength(320);
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();

        builder.HasIndex(c => c.EstabelecimentoId).IsUnique()
            .HasDatabaseName("uq_configuracoes_automacao_estabelecimento");

        builder.HasOne<Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_configuracao_automacao_estabelecimento");

        builder.Ignore(c => c.DomainEvents);
    }
}
