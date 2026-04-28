using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Vinculos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class VinculoProfissionalEstabelecimentoConfiguration : IEntityTypeConfiguration<VinculoProfissionalEstabelecimento>
{
    public void Configure(EntityTypeBuilder<VinculoProfissionalEstabelecimento> builder)
    {
        builder.ToTable("vinculo_profissional_estabelecimento");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(v => v.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(v => v.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(v => v.ModeloPermissaoId).HasColumnName("modelo_permissao_id").IsRequired();
        builder.Property(v => v.ConvidadoPorUsuarioId).HasColumnName("convidado_por_usuario_id").IsRequired();
        builder.Property(v => v.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.ConvidadoEm).HasColumnName("convidado_em").IsRequired();
        builder.Property(v => v.AceitoEm).HasColumnName("aceito_em");
        builder.Property(v => v.InativadoEm).HasColumnName("inativado_em");

        builder.HasIndex(v => new { v.EstabelecimentoId, v.Status }).HasDatabaseName("ix_vinculo_estabelecimento_status");
        builder.HasIndex(v => new { v.ProfissionalUsuarioId, v.Status }).HasDatabaseName("ix_vinculo_profissional_status");

        builder.Ignore(v => v.DomainEvents);
    }
}
