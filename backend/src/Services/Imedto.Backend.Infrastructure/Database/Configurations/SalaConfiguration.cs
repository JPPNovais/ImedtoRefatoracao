using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Unidades;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class SalaConfiguration : IEntityTypeConfiguration<Sala>
{
    public void Configure(EntityTypeBuilder<Sala> builder)
    {
        builder.ToTable("sala_atendimento");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(s => s.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(s => s.UnidadeId).HasColumnName("unidade_id").IsRequired();
        builder.Property(s => s.TipoSalaId).HasColumnName("tipo_sala_id");
        builder.Property(s => s.Nome).HasColumnName("nome").IsRequired().HasMaxLength(200);
        builder.Property(s => s.Descricao).HasColumnName("descricao").HasMaxLength(500);
        builder.Property(s => s.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(s => s.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(s => s.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(s => s.EstabelecimentoId).HasDatabaseName("ix_salas_estab");
        builder.HasIndex(s => s.UnidadeId).HasDatabaseName("ix_salas_unidade");

        // FKs (sem navigation property — controle de tudo via aggregate handler).
        builder.HasOne<Estabelecimento>()
            .WithMany()
            .HasForeignKey(s => s.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_salas_estab");

        builder.HasOne<UnidadeEstabelecimento>()
            .WithMany()
            .HasForeignKey(s => s.UnidadeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_salas_unidade");

        builder.HasOne<TipoSala>()
            .WithMany()
            .HasForeignKey(s => s.TipoSalaId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_salas_tipo");

        builder.Ignore(s => s.DomainEvents);
    }
}
