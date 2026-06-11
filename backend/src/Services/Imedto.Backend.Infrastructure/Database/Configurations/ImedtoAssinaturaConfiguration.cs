using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoAssinaturaConfiguration : IEntityTypeConfiguration<ImedtoAssinatura>
{
    public void Configure(EntityTypeBuilder<ImedtoAssinatura> builder)
    {
        builder.ToTable("imedto_assinaturas");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.PlanoId).HasColumnName("plano_id").IsRequired();
        builder.Property(a => a.IniciadaEm).HasColumnName("iniciada_em").IsRequired();
        builder.Property(a => a.FimEm).HasColumnName("fim_em");
        builder.Property(a => a.ExpiraEm).HasColumnName("expira_em");
        builder.Property(a => a.SuspensaEm).HasColumnName("suspensa_em");
        builder.Property(a => a.Origem).HasColumnName("origem").HasDefaultValue("admin_manual").IsRequired();
        builder.Property(a => a.ReferenciaExterna).HasColumnName("referencia_externa");
        builder.Property(a => a.StatusCobranca).HasColumnName("status_cobranca").HasDefaultValue("nao_aplicavel").IsRequired();
        builder.Property(a => a.Gratuita).HasColumnName("gratuita").HasDefaultValue(false).IsRequired();
        builder.Property(a => a.Motivo).HasColumnName("motivo");
        builder.Property(a => a.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(a => a.CriadaPorAdminId).HasColumnName("criada_por_admin_id");

        // Índice principal: "qual a assinatura vigente do tenant?" → estabelecimento_id + fim_em.
        // fim_em IS NULL = vigente. Ordenar por iniciada_em DESC para pegar a mais recente.
        builder.HasIndex(a => new { a.EstabelecimentoId, a.FimEm })
            .HasDatabaseName("ix_imedto_assinaturas_estabelecimento_fim");

        // Índice para varreduras de expiração (job de expirar trials vencidos).
        builder.HasIndex(a => a.ExpiraEm)
            .HasDatabaseName("ix_imedto_assinaturas_expira_em");

        // Índice em plano_id: "quantos tenants usam o plano X?".
        builder.HasIndex(a => a.PlanoId).HasDatabaseName("ix_imedto_assinaturas_plano");

        // FK para imedto_planos com RESTRICT: não deletar plano com assinaturas.
        builder.HasOne<ImedtoPlano>()
            .WithMany()
            .HasForeignKey(a => a.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK para admin criador (nullable — futuro self-signup não terá admin associado).
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(a => a.CriadaPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK para estabelecimentos.id (bigint) com RESTRICT: não deletar estabelecimento com histórico de assinaturas.
        // Briefing §7: "deletar estabelecimento NÃO deve apagar o histórico".
        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(a => a.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.DomainEvents);
    }
}
