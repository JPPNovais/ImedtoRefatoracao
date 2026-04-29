using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoCirurgiaConfiguration : IEntityTypeConfiguration<OrcamentoCirurgia>
{
    public void Configure(EntityTypeBuilder<OrcamentoCirurgia> builder)
    {
        builder.ToTable("orcamento_cirurgias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(c => c.OrcamentoId).HasColumnName("orcamento_id").IsRequired();
        builder.Property(c => c.ProcedimentoCirurgicoId).HasColumnName("procedimento_cirurgico_id");
        builder.Property(c => c.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Quantidade).HasColumnName("quantidade").IsRequired().HasDefaultValue(1);
        builder.Property(c => c.DuracaoMinutos).HasColumnName("duracao_minutos");
        builder.Property(c => c.ValorTotal).HasColumnName("valor_total").HasPrecision(12, 2).IsRequired();
        builder.Property(c => c.Ordem).HasColumnName("ordem").IsRequired();

        builder.Ignore(c => c.DomainEvents);

        // FK opcional para o catálogo de procedimentos. SET NULL preserva snapshot do
        // orçamento se o procedimento for removido depois.
        builder.HasOne<Domain.Cirurgias.ProcedimentoCirurgico>()
            .WithMany()
            .HasForeignKey(c => c.ProcedimentoCirurgicoId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_orcamento_cirurgia_procedimento_cirurgico");

        builder.HasIndex(c => c.OrcamentoId)
            .HasDatabaseName("ix_orcamento_cirurgia_orcamento");
    }
}
