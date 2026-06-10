using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Cobrancas;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cobrancas;

public class TabelaPrecoConsultaConfiguration : IEntityTypeConfiguration<TabelaPrecoConsulta>
{
    public void Configure(EntityTypeBuilder<TabelaPrecoConsulta> builder)
    {
        builder.ToTable("tabela_preco_consulta");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(t => t.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(t => t.ProfissionalId).HasColumnName("profissional_id");
        builder.Property(t => t.ValorSugerido).HasColumnName("valor_sugerido").HasPrecision(12, 2).IsRequired();
        builder.Property(t => t.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(t => t.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => new { t.EstabelecimentoId, t.ProfissionalId })
            .HasDatabaseName("ix_tabela_preco_consulta_estab_profissional");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(t => t.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tabela_preco_consulta_estabelecimento");
    }
}
