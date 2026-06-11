using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Convenios;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Convenios;

public class ConvenioPlanoConfiguration : IEntityTypeConfiguration<ConvenioPlano>
{
    public void Configure(EntityTypeBuilder<ConvenioPlano> builder)
    {
        builder.ToTable("convenio_planos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(p => p.ConvenioId).HasColumnName("convenio_id").IsRequired();
        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(p => p.Nome).HasColumnName("nome").IsRequired();
        builder.Property(p => p.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(p => new { p.ConvenioId, p.Ativo })
            .HasDatabaseName("ix_convenio_planos_convenio_ativo");

        // Índice explícito na FK de estabelecimento (padrão anti-FK-sem-índice).
        builder.HasIndex(p => p.EstabelecimentoId)
            .HasDatabaseName("ix_convenio_planos_estabelecimento_id");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(p => p.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_convenio_planos_estabelecimento");
    }
}
