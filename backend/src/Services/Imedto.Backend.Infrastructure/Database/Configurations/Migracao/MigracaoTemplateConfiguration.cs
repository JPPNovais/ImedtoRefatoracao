using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Database.Configurations.Migracao;

/// <summary>
/// Mapping EF para migracao_templates — tabela cross-tenant (sem estabelecimento_id).
/// Justificativa global: metadado de schema compartilhado entre todos os tenants.
/// UNIQUE (nome, entidade) — 1 template por sistema de origem + tipo de entidade.
/// </summary>
public class MigracaoTemplateConfiguration : IEntityTypeConfiguration<MigracaoTemplate>
{
    public void Configure(EntityTypeBuilder<MigracaoTemplate> builder)
    {
        builder.ToTable("migracao_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("bigint")
            .UseIdentityByDefaultColumn();

        builder.Property(t => t.Nome)
            .HasColumnName("nome")
            .HasColumnType("varchar(200)")
            .IsRequired();

        builder.Property(t => t.Entidade)
            .HasColumnName("entidade")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(t => t.MapaJson)
            .HasColumnName("mapa_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(t => t.CriadoPorUsuarioId)
            .HasColumnName("criado_por_usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // UNIQUE (nome, entidade) — 1 template por sistema de origem + tipo de entidade.
        builder.HasIndex(t => new { t.Nome, t.Entidade })
            .IsUnique()
            .HasDatabaseName("uq_migracao_templates_nome_entidade");

        builder.Ignore(t => t.DomainEvents);
    }
}
