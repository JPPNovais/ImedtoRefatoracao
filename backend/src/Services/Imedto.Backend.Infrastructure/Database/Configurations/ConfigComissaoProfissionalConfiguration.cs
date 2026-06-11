using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Financeiro;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ConfigComissaoProfissionalConfiguration : IEntityTypeConfiguration<ConfigComissaoProfissional>
{
    public void Configure(EntityTypeBuilder<ConfigComissaoProfissional> builder)
    {
        builder.ToTable("config_comissao_profissional");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(c => c.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(c => c.Percentual).HasColumnName("percentual").HasPrecision(5, 2).IsRequired();

        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        // UNIQUE (estabelecimento_id, profissional_usuario_id, tipo) — 1 config por combinação.
        builder.HasIndex(c => new { c.EstabelecimentoId, c.ProfissionalUsuarioId, c.Tipo })
            .IsUnique()
            .HasDatabaseName("uq_config_comissao_estab_prof_tipo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_config_comissao_estabelecimento");

        builder.Ignore(c => c.DomainEvents);
    }
}
