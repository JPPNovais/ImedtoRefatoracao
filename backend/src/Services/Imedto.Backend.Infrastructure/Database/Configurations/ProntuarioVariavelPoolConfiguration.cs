using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProntuarioVariavelPoolConfiguration : IEntityTypeConfiguration<ProntuarioVariavelPool>
{
    public void Configure(EntityTypeBuilder<ProntuarioVariavelPool> builder)
    {
        builder.ToTable("prontuario_variaveis_pool");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id");
        builder.Property(p => p.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Nome).HasColumnName("nome").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(p => p.EhPadraoSistema).HasColumnName("eh_padrao_sistema").IsRequired();
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(p => new { p.EstabelecimentoId, p.Tipo })
            .HasDatabaseName("ix_pool_estabelecimento_tipo");
        builder.HasIndex(p => new { p.EhPadraoSistema, p.Tipo })
            .HasDatabaseName("ix_pool_padrao_tipo");

        builder.Ignore(p => p.DomainEvents);
    }
}
