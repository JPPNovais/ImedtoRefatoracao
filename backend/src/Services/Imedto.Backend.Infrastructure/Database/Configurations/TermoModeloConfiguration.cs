using Imedto.Backend.Domain.Termos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class TermoModeloConfiguration : IEntityTypeConfiguration<TermoModelo>
{
    public void Configure(EntityTypeBuilder<TermoModelo> builder)
    {
        builder.ToTable("termo_modelo");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(m => m.Categoria).HasColumnName("categoria").HasMaxLength(30).HasConversion<string>().IsRequired();
        builder.Property(m => m.Titulo).HasColumnName("titulo").HasMaxLength(TermoModelo.TituloMaximo).IsRequired();
        builder.Property(m => m.ConteudoHtml).HasColumnName("conteudo_html").HasColumnType("text").IsRequired();
        builder.Property(m => m.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(m => m.VersaoAtual).HasColumnName("versao_atual").IsRequired();
        builder.Property(m => m.PadraoClonadoDeId).HasColumnName("padrao_clonado_de");
        builder.Property(m => m.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id");
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");

        // Optimistic concurrency via xmin do Postgres (uint32, system column).
        builder.Property(m => m.VersaoConcorrencia)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Listagem por estabelecimento (inclui padroes IS NULL via Coalesce/Where).
        builder.HasIndex(m => new { m.EstabelecimentoId, m.Categoria, m.Ativo })
            .HasDatabaseName("ix_termo_modelo_estab_cat_ativo");

        // Padroes do sistema (estabelecimento_id IS NULL) — filtro parcial.
        builder.HasIndex(m => m.Categoria)
            .HasDatabaseName("ix_termo_modelo_padrao_categoria")
            .HasFilter("estabelecimento_id IS NULL");

        // Soft delete — apenas registros ativos no plano comum.
        builder.HasIndex(m => new { m.EstabelecimentoId, m.DeletadoEm })
            .HasDatabaseName("ix_termo_modelo_estab_deletado");

        builder.Ignore(m => m.DomainEvents);
        builder.Ignore(m => m.EhPadraoDoSistema);
        builder.Ignore(m => m.EstaDeletado);
    }
}

public class TermoModeloVersaoConfiguration : IEntityTypeConfiguration<TermoModeloVersao>
{
    public void Configure(EntityTypeBuilder<TermoModeloVersao> builder)
    {
        builder.ToTable("termo_modelo_versao");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(v => v.TermoModeloId).HasColumnName("termo_modelo_id").IsRequired();
        builder.Property(v => v.Versao).HasColumnName("versao").IsRequired();
        builder.Property(v => v.ConteudoHtml).HasColumnName("conteudo_html").HasColumnType("text").IsRequired();
        builder.Property(v => v.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(v => v.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id");

        // Unicidade por (modelo, versao).
        builder.HasIndex(v => new { v.TermoModeloId, v.Versao })
            .IsUnique()
            .HasDatabaseName("uq_termo_modelo_versao");

        builder.Ignore(v => v.DomainEvents);
    }
}
