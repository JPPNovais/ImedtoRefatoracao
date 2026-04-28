using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Unidades;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class UnidadeEstabelecimentoConfiguration : IEntityTypeConfiguration<UnidadeEstabelecimento>
{
    public void Configure(EntityTypeBuilder<UnidadeEstabelecimento> builder)
    {
        builder.ToTable("unidades_estabelecimento");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(u => u.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(u => u.Nome).HasColumnName("nome").IsRequired().HasMaxLength(200);
        builder.Property(u => u.IsPrincipal).HasColumnName("is_principal").IsRequired();
        builder.Property(u => u.Cep).HasColumnName("cep").HasMaxLength(8);
        builder.Property(u => u.Logradouro).HasColumnName("logradouro").HasMaxLength(200);
        builder.Property(u => u.Numero).HasColumnName("numero").HasMaxLength(20);
        builder.Property(u => u.Complemento).HasColumnName("complemento").HasMaxLength(100);
        builder.Property(u => u.Bairro).HasColumnName("bairro").HasMaxLength(100);
        builder.Property(u => u.Cidade).HasColumnName("cidade").HasMaxLength(100);
        builder.Property(u => u.Estado).HasColumnName("estado").HasMaxLength(2);
        builder.Property(u => u.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(u => u.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(u => u.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(u => u.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(u => u.EstabelecimentoId, "ix_unidades_estab");

        // Apenas uma unidade principal por estabelecimento.
        builder.HasIndex(u => u.EstabelecimentoId, "uq_unidades_principal_por_estab")
            .IsUnique()
            .HasFilter("is_principal = true");

        // FK → estabelecimentos (cascade: ao deletar o tenant root, unidades vão junto).
        builder.HasOne<Estabelecimento>()
            .WithMany()
            .HasForeignKey(u => u.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_unidades_estabelecimento");

        builder.Ignore(u => u.DomainEvents);
    }
}
