using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Catalogo;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class EspecialidadeConfiguration : IEntityTypeConfiguration<Especialidade>
{
    public void Configure(EntityTypeBuilder<Especialidade> builder)
    {
        builder.ToTable("especialidades");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(e => e.ProfissaoId).HasColumnName("profissao_id").IsRequired();
        builder.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        builder.Property(e => e.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);

        builder.HasIndex(e => new { e.ProfissaoId, e.Nome }).IsUnique()
            .HasDatabaseName("uq_especialidades_profissao_nome");
        builder.HasIndex(e => new { e.ProfissaoId, e.Ativo })
            .HasDatabaseName("ix_especialidades_profissao_ativo");

        builder.HasOne<Profissao>()
            .WithMany()
            .HasForeignKey(e => e.ProfissaoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_especialidades_profissao");

        builder.Ignore(e => e.DomainEvents);
    }
}
