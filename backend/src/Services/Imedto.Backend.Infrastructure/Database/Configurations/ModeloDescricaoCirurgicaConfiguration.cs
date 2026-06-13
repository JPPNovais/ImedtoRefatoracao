using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ModeloDescricaoCirurgicaConfiguration : IEntityTypeConfiguration<ModeloDescricaoCirurgica>
{
    public void Configure(EntityTypeBuilder<ModeloDescricaoCirurgica> builder)
    {
        builder.ToTable("modelos_descricao_cirurgica");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(m => m.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(m => m.Titulo).HasColumnName("titulo").IsRequired().HasMaxLength(200);
        builder.Property(m => m.Corpo).HasColumnName("corpo").IsRequired();
        builder.Property(m => m.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(m => m.EhPadraoSistema).HasColumnName("eh_padrao_sistema").IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(m => m.EstabelecimentoId)
            .HasDatabaseName("ix_modelo_desc_cirurgica_estabelecimento");
        builder.HasIndex(m => m.EhPadraoSistema)
            .HasDatabaseName("ix_modelo_desc_cirurgica_padrao");

        builder.Ignore(m => m.DomainEvents);
    }
}
