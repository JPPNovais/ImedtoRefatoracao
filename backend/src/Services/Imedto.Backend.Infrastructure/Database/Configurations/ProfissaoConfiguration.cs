using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Catalogo;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProfissaoConfiguration : IEntityTypeConfiguration<Profissao>
{
    public void Configure(EntityTypeBuilder<Profissao> builder)
    {
        builder.ToTable("profissoes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        builder.Property(p => p.ConselhoSigla).HasColumnName("conselho_sigla").HasMaxLength(10);
        builder.Property(p => p.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);

        builder.HasIndex(p => p.Nome).IsUnique().HasDatabaseName("uq_profissoes_nome");
        builder.HasIndex(p => p.Ativo).HasDatabaseName("ix_profissoes_ativo");

        builder.Ignore(p => p.DomainEvents);
    }
}
