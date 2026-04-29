using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Automacoes;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class EventoAutomacaoConfiguration : IEntityTypeConfiguration<EventoAutomacao>
{
    public void Configure(EntityTypeBuilder<EventoAutomacao> builder)
    {
        builder.ToTable("automation_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(e => e.RegraId).HasColumnName("regra_id").IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(e => e.TentativaN).HasColumnName("tentativa_n").IsRequired();
        builder.Property(e => e.ExecutarEm).HasColumnName("executar_em").IsRequired();
        builder.Property(e => e.ExecutadoEm).HasColumnName("executado_em");
        builder.Property(e => e.UltimaFalha).HasColumnName("ultima_falha").HasMaxLength(500);
        builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasOne<RegraAutomacao>()
            .WithMany()
            .HasForeignKey(e => e.RegraId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_automation_events_regra");

        // Worker poll: ListarPendentesProntos.
        builder.HasIndex(e => new { e.Status, e.ExecutarEm })
            .HasDatabaseName("ix_automation_events_status_executar_em");

        builder.Ignore(e => e.DomainEvents);
    }
}
