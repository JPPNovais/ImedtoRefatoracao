using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ModeloDeProntuarioConfiguration : IEntityTypeConfiguration<ModeloDeProntuario>
{
    public void Configure(EntityTypeBuilder<ModeloDeProntuario> builder)
    {
        builder.ToTable("modelo_de_prontuario");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(m => m.Nome).HasColumnName("nome").IsRequired().HasMaxLength(200);
        builder.Property(m => m.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(m => m.EstruturaJson).HasColumnName("estrutura").HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.EhPadraoSistema).HasColumnName("eh_padrao_sistema").IsRequired();
        builder.Property(m => m.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(m => m.EstabelecimentoId).HasDatabaseName("ix_modelo_prontuario_estabelecimento");
        builder.HasIndex(m => m.EhPadraoSistema).HasDatabaseName("ix_modelo_prontuario_padrao_sistema");

        builder.Ignore(m => m.DomainEvents);
    }
}
