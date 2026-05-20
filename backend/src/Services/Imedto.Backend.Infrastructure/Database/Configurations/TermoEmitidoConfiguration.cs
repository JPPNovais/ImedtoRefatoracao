using Imedto.Backend.Domain.Termos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class TermoEmitidoConfiguration : IEntityTypeConfiguration<TermoEmitido>
{
    public void Configure(EntityTypeBuilder<TermoEmitido> builder)
    {
        builder.ToTable("termo_emitido");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(t => t.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(t => t.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(t => t.TermoModeloId).HasColumnName("termo_modelo_id").IsRequired();
        builder.Property(t => t.VersaoModelo).HasColumnName("versao_modelo").IsRequired();
        builder.Property(t => t.ConteudoSnapshotHtml).HasColumnName("conteudo_snapshot_html").HasColumnType("text").IsRequired();
        builder.Property(t => t.ConteudoSnapshotTexto).HasColumnName("conteudo_snapshot_texto").HasColumnType("text").IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(t => t.AssinaturaTipo).HasColumnName("assinatura_tipo").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(t => t.AssinadoEm).HasColumnName("assinado_em");
        builder.Property(t => t.IpAssinatura).HasColumnName("ip_assinatura").HasMaxLength(45);
        builder.Property(t => t.UserAgentAssinatura).HasColumnName("user_agent_assinatura").HasMaxLength(500);
        builder.Property(t => t.HashIntegridade).HasColumnName("hash_integridade").HasMaxLength(64).IsRequired().IsFixedLength();
        builder.Property(t => t.PdfUrl).HasColumnName("pdf_url").HasMaxLength(500);
        builder.Property(t => t.PdfHash).HasColumnName("pdf_hash").HasMaxLength(64).IsFixedLength();
        builder.Property(t => t.TokenAceite).HasColumnName("token_aceite").HasMaxLength(64);
        builder.Property(t => t.TokenExpiraEm).HasColumnName("token_expira_em");
        builder.Property(t => t.RevogadoEm).HasColumnName("revogado_em");
        builder.Property(t => t.RevogadoPorUsuarioId).HasColumnName("revogado_por_usuario_id");
        builder.Property(t => t.RevogadoMotivo).HasColumnName("revogado_motivo").HasMaxLength(TermoEmitido.MotivoRevogacaoMaximo);
        builder.Property(t => t.EmitidoPorUsuarioId).HasColumnName("emitido_por_usuario_id").IsRequired();
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(t => t.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(t => new { t.PacienteId, t.EstabelecimentoId, t.CriadoEm })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_termo_emitido_paciente_estab_criado");

        builder.HasIndex(t => new { t.EstabelecimentoId, t.Status })
            .HasDatabaseName("ix_termo_emitido_estab_status");

        // Token só faz sentido único quando NOT NULL (link público).
        builder.HasIndex(t => t.TokenAceite)
            .IsUnique()
            .HasFilter("token_aceite IS NOT NULL")
            .HasDatabaseName("uq_termo_emitido_token");

        builder.Ignore(t => t.DomainEvents);
    }
}

public class TermoEmitidoAcessoLogConfiguration : IEntityTypeConfiguration<TermoEmitidoAcessoLog>
{
    public void Configure(EntityTypeBuilder<TermoEmitidoAcessoLog> builder)
    {
        builder.ToTable("termo_emitido_acesso_log");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(l => l.TermoEmitidoId).HasColumnName("termo_emitido_id").IsRequired();
        builder.Property(l => l.IpOrigem).HasColumnName("ip_origem").HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(l => l.Acao).HasColumnName("acao").HasMaxLength(30).IsRequired();
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();

        builder.HasIndex(l => new { l.TermoEmitidoId, l.CriadoEm })
            .IsDescending(false, true)
            .HasDatabaseName("ix_termo_emitido_acesso_log_termo_criado");

        builder.Ignore(l => l.DomainEvents);
    }
}
