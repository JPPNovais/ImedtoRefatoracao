using Imedto.Backend.Domain.Inventario.Cadastros;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Cadastros;

public class LocalEstoqueConfiguration : IEntityTypeConfiguration<LocalEstoque>
{
    public void Configure(EntityTypeBuilder<LocalEstoque> builder)
    {
        builder.ToTable("locais_estoque");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(l => l.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(l => l.Nome).HasColumnName("nome").HasMaxLength(120).IsRequired();
        // Enum persistido como string para legibilidade no DB e schema explícito.
        builder.Property(l => l.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(l => l.AndarSetor).HasColumnName("andar_setor").HasMaxLength(80);
        builder.Property(l => l.Responsavel).HasColumnName("responsavel").HasMaxLength(150);
        builder.Property(l => l.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(l => l.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(l => new { l.EstabelecimentoId, l.Ativo })
            .HasDatabaseName("ix_locais_estoque_estab_ativo");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(l => l.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_locais_estoque_estabelecimento");

        builder.Ignore(l => l.DomainEvents);
    }
}
