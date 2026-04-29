using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.ModelosPermissao;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ModeloPermissaoEstabelecimentoConfiguration : IEntityTypeConfiguration<ModeloPermissaoEstabelecimento>
{
    public void Configure(EntityTypeBuilder<ModeloPermissaoEstabelecimento> builder)
    {
        builder.ToTable("modelo_permissao_estabelecimento");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(m => m.Nome).HasColumnName("nome").IsRequired().HasMaxLength(100);
        builder.Property(m => m.TipoAcesso)
            .HasColumnName("tipo_acesso")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(m => m.PermissoesJson).HasColumnName("permissoes").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.PermissoesExtrasJson)
            .HasColumnName("permissoes_extras")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");
        // Read-only typed accessors — não são colunas.
        builder.Ignore(m => m.Permissoes);
        builder.Ignore(m => m.PermissoesExtrasLista);
        builder.Property(m => m.EhPadrao).HasColumnName("eh_padrao").IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(m => m.EstabelecimentoId).HasDatabaseName("ix_modelo_permissao_estabelecimento");
        builder.HasIndex(m => new { m.EstabelecimentoId, m.Nome }).IsUnique()
            .HasDatabaseName("uq_modelo_permissao_nome_por_estabelecimento");

        builder.Ignore(m => m.DomainEvents);
    }
}
