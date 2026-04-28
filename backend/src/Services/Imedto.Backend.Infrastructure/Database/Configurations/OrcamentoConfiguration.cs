using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamentos");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(o => o.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(o => o.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(o => o.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(o => o.Validade).HasColumnName("validade").IsRequired();
        builder.Property(o => o.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(o => o.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(o => o.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(o => o.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);

        builder.HasMany(o => o.Itens)
            .WithOne()
            .HasForeignKey(i => i.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_item_orcamento_orcamento");

        builder.HasIndex(o => new { o.EstabelecimentoId, o.Status })
            .HasDatabaseName("ix_orcamento_estab_status");
        builder.HasIndex(o => new { o.EstabelecimentoId, o.PacienteId })
            .HasDatabaseName("ix_orcamento_estab_paciente");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(o => o.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_orcamento_estabelecimento");

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(o => o.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_orcamento_paciente");
    }
}
