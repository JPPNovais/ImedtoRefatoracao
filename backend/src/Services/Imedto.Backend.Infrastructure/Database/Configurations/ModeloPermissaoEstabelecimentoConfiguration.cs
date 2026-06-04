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

        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired(false);
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
        builder.Property(m => m.Icone).HasColumnName("icone").HasMaxLength(50);
        builder.Property(m => m.Cor).HasColumnName("cor").HasMaxLength(40);
        builder.Property(m => m.Descricao).HasColumnName("descricao").HasMaxLength(200);
        // Read-only typed accessors — não são colunas.
        builder.Ignore(m => m.Permissoes);
        builder.Ignore(m => m.PermissoesExtrasLista);
        builder.Property(m => m.EhPadrao).HasColumnName("eh_padrao").IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(m => m.EstabelecimentoId).HasDatabaseName("ix_modelo_permissao_estabelecimento");
        builder.HasIndex(m => new { m.EstabelecimentoId, m.Nome }).IsUnique()
            .HasDatabaseName("uq_modelo_permissao_nome_por_estabelecimento");

        // Unique parcial para o escopo global (WHERE estabelecimento_id IS NULL):
        // garante que não existam dois registros globais com o mesmo nome.
        // O EF Core não suporta natively unique parcial — o índice é criado via migration SQL custom.
        // Ver db/migrations/*_modelos_permissao_padrao_sistema.sql

        builder.Ignore(m => m.DomainEvents);
    }
}
