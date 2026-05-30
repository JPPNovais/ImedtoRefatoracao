using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ImedtoPlanoConfiguration : IEntityTypeConfiguration<ImedtoPlano>
{
    public void Configure(EntityTypeBuilder<ImedtoPlano> builder)
    {
        builder.ToTable("imedto_planos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Nome).HasColumnName("nome").IsRequired();
        builder.Property(p => p.DescricaoCurta).HasColumnName("descricao_curta");
        builder.Property(p => p.PrecoMensalCentavos).HasColumnName("preco_mensal_centavos");
        builder.Property(p => p.Gratuito).HasColumnName("gratuito").HasDefaultValue(false).IsRequired();
        builder.Property(p => p.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(p => p.LimitesJson).HasColumnName("limites_json").HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.CriadoPorAdminId).HasColumnName("criado_por_admin_id");

        // Nome único: não pode existir dois planos com mesmo nome.
        builder.HasIndex(p => p.Nome).IsUnique().HasDatabaseName("uq_imedto_planos_nome");

        // Índice em ativo: filtro frequente ao listar planos disponíveis.
        builder.HasIndex(p => p.Ativo).HasDatabaseName("ix_imedto_planos_ativo");

        // FK para admin criador (nullable — seed inicial pode não ter admin logado ainda).
        builder.HasOne<ImedtoAdmin>()
            .WithMany()
            .HasForeignKey(p => p.CriadoPorAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(p => p.DomainEvents);
    }
}
