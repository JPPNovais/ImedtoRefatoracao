using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Assinaturas;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> builder)
    {
        builder.ToTable("assinaturas");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.PlanoId).HasColumnName("plano_id").IsRequired();
        builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion<string>().IsRequired();
        builder.Property(a => a.IniciadaEm).HasColumnName("iniciada_em").IsRequired();
        builder.Property(a => a.ExpiraEm).HasColumnName("expira_em");
        builder.Property(a => a.CanceladaEm).HasColumnName("cancelada_em");
        builder.Property(a => a.RenovadaEm).HasColumnName("renovada_em");
        builder.Property(a => a.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(a => a.AtualizadaEm).HasColumnName("atualizada_em");

        // 1:1 com estabelecimento (regra do schema fechado).
        builder.HasIndex(a => a.EstabelecimentoId).IsUnique().HasDatabaseName("uq_assinaturas_estabelecimento");

        // Cobertura do job de expirar trials: filtra status + expira_em.
        builder.HasIndex(a => new { a.Status, a.ExpiraEm }).HasDatabaseName("ix_assinaturas_status_expira");

        // FKs explícitas (sem navegações no aggregate — só ids).
        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(a => a.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Plano>()
            .WithMany()
            .HasForeignKey(a => a.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.DomainEvents);
    }
}
