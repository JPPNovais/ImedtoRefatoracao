using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Assinaturas;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class PlanoConfiguration : IEntityTypeConfiguration<Plano>
{
    public void Configure(EntityTypeBuilder<Plano> builder)
    {
        builder.ToTable("planos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
        builder.Property(p => p.PrecoMensal).HasColumnName("preco_mensal").HasColumnType("numeric(12,2)")
            .HasDefaultValue(0m).IsRequired();
        builder.Property(p => p.LimiteProfissionais).HasColumnName("limite_profissionais");
        builder.Property(p => p.LimitePacientes).HasColumnName("limite_pacientes");
        builder.Property(p => p.FeaturesJson).HasColumnName("features_json").HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(p => p.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(p => p.Ordem).HasColumnName("ordem").HasDefaultValue(0).IsRequired();

        // Nome do plano é único — usado pelo seed e pelo handler de trial.
        builder.HasIndex(p => p.Nome).IsUnique().HasDatabaseName("uq_planos_nome");

        builder.Ignore(p => p.DomainEvents);
    }
}
