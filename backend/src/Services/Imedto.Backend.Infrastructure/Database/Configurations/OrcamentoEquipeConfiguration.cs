using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoEquipeConfiguration : IEntityTypeConfiguration<OrcamentoEquipe>
{
    public void Configure(EntityTypeBuilder<OrcamentoEquipe> builder)
    {
        builder.ToTable("orcamento_equipe");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(e => e.OrcamentoId).HasColumnName("orcamento_id").IsRequired();
        builder.Property(e => e.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(e => e.Papel).HasColumnName("papel").HasMaxLength(40).IsRequired();
        builder.Property(e => e.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.Ordem).HasColumnName("ordem").IsRequired();

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => new { e.OrcamentoId, e.ProfissionalUsuarioId, e.Papel })
            .IsUnique()
            .HasDatabaseName("uq_orcamento_equipe_orcamento_profissional_papel");
    }
}
