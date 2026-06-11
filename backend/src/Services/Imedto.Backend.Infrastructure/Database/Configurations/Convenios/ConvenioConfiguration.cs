using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Convenios;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Convenios;

public class ConvenioConfiguration : IEntityTypeConfiguration<Convenio>
{
    public void Configure(EntityTypeBuilder<Convenio> builder)
    {
        builder.ToTable("convenios");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(c => c.Nome).HasColumnName("nome").IsRequired();
        builder.Property(c => c.RegistroAns).HasColumnName("registro_ans");
        builder.Property(c => c.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => new { c.EstabelecimentoId, c.Ativo })
            .HasDatabaseName("ix_convenios_estab_ativo");

        builder.HasMany(c => c.Planos)
            .WithOne()
            .HasForeignKey(p => p.ConvenioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_convenio_planos_convenio");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(c => c.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_convenios_estabelecimento");
    }
}
