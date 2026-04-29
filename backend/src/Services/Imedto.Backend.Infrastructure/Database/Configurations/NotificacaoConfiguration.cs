using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Notificacoes;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("notificacoes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(n => n.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(n => n.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(n => n.Titulo).HasColumnName("titulo").HasMaxLength(200).IsRequired();
        builder.Property(n => n.Mensagem).HasColumnName("mensagem").HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Categoria).HasColumnName("categoria").HasMaxLength(40).IsRequired()
            .HasConversion<string>();
        builder.Property(n => n.LinkAcao).HasColumnName("link_acao").HasMaxLength(500);
        builder.Property(n => n.Lida).HasColumnName("lida").IsRequired();
        builder.Property(n => n.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(n => n.LidaEm).HasColumnName("lida_em");

        // Cobertura do caso de uso principal: sino do usuário, ordenado por mais recente.
        builder.HasIndex(n => new { n.UsuarioId, n.Lida, n.CriadaEm })
            .HasDatabaseName("ix_notificacoes_usuario_lida_criada");

        // Auditoria/relatório por estabelecimento.
        builder.HasIndex(n => new { n.EstabelecimentoId, n.CriadaEm })
            .HasDatabaseName("ix_notificacoes_estabelecimento_criada");

        builder.Ignore(n => n.DomainEvents);
    }
}
