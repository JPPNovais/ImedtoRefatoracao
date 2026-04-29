using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class EstabelecimentoIaSettingsConfiguration : IEntityTypeConfiguration<EstabelecimentoIaSettings>
{
    public void Configure(EntityTypeBuilder<EstabelecimentoIaSettings> builder)
    {
        builder.ToTable("establishment_ai_settings");

        // PK = FK para estabelecimentos.id (1:1 com tenant). Sem identity — o caller
        // sempre fornece o id (= estabelecimento_id).
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("estabelecimento_id").ValueGeneratedNever();

        builder.Property(s => s.AiEnabled)
            .HasColumnName("ai_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.AiProvider)
            .HasColumnName("ai_provider")
            .HasMaxLength(40)
            .IsRequired()
            .HasDefaultValue("anthropic");

        builder.Property(s => s.AiModel)
            .HasColumnName("ai_model")
            .HasMaxLength(80)
            .IsRequired()
            .HasDefaultValue("claude-sonnet-4-6");

        builder.Property(s => s.RateLimitPerMinute)
            .HasColumnName("rate_limit_per_minute")
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(s => s.RateLimitPerDay)
            .HasColumnName("rate_limit_per_day")
            .IsRequired()
            .HasDefaultValue(200);

        // Persiste como string ('standard'/'minimized') para casar com o schema do doc.
        builder.Property(s => s.DataMinimizationLevel)
            .HasColumnName("data_minimization_level")
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(NivelMinimizacaoDados.Standard)
            .HasConversion(
                v => v == NivelMinimizacaoDados.Minimized ? "minimized" : "standard",
                v => v == "minimized" ? NivelMinimizacaoDados.Minimized : NivelMinimizacaoDados.Standard);

        builder.Property(s => s.AtualizadaEm).HasColumnName("atualizada_em");

        // FK para estabelecimentos com ON DELETE CASCADE — quando o tenant some, o registro vai junto.
        builder.HasOne<Estabelecimento>()
            .WithOne()
            .HasForeignKey<EstabelecimentoIaSettings>(s => s.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
