using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProntuarioAnexoConfiguration : IEntityTypeConfiguration<ProntuarioAnexo>
{
    public void Configure(EntityTypeBuilder<ProntuarioAnexo> builder)
    {
        builder.ToTable("prontuario_anexos");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(a => a.ProntuarioId).HasColumnName("prontuario_id").IsRequired();
        builder.Property(a => a.EvolucaoId).HasColumnName("evolucao_id");
        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.StoragePath).HasColumnName("storage_path").IsRequired().HasMaxLength(500);
        builder.Property(a => a.NomeOriginal).HasColumnName("nome_original").IsRequired().HasMaxLength(300);
        builder.Property(a => a.MimeType).HasColumnName("mime_type").IsRequired().HasMaxLength(100);
        builder.Property(a => a.TamanhoBytes).HasColumnName("tamanho_bytes").IsRequired();
        builder.Property(a => a.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(a => a.ArquivadoEm).HasColumnName("arquivado_em");
        builder.Property(a => a.ArquivadoPorUsuarioId).HasColumnName("arquivado_por_usuario_id");

        builder.HasIndex(a => new { a.ProntuarioId, a.ArquivadoEm })
            .HasDatabaseName("ix_anexos_prontuario");
        builder.HasIndex(a => a.EvolucaoId).HasDatabaseName("ix_anexos_evolucao");

        builder.Ignore(a => a.DomainEvents);
        builder.Ignore(a => a.EstaArquivado);
    }
}
