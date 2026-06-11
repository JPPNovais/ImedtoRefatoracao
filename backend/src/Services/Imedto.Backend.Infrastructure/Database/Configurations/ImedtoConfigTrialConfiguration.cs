using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoConfigTrialConfiguration : IEntityTypeConfiguration<ImedtoConfigTrial>
{
    public void Configure(EntityTypeBuilder<ImedtoConfigTrial> builder)
    {
        builder.ToTable("imedto_config_trial");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.PlanoTrialId).HasColumnName("plano_trial_id").IsRequired();
        builder.Property(c => c.DuracaoTrialDias).HasColumnName("duracao_trial_dias")
            .HasDefaultValue(14).IsRequired();
        builder.Property(c => c.TrialHabilitado).HasColumnName("trial_habilitado")
            .HasDefaultValue(true).IsRequired();
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();
        builder.Property(c => c.AtualizadoPorUsuarioId).HasColumnName("atualizado_por_usuario_id");

        // FK para imedto_planos com RESTRICT: não deletar plano enquanto for o plano de trial.
        builder.HasOne<ImedtoPlano>()
            .WithMany()
            .HasForeignKey(c => c.PlanoTrialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.DomainEvents);
    }
}
