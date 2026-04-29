using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Vinculos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

/// <summary>
/// Mapeamento da tabela <c>solicitacoes_vinculo</c>. Schema final + RLS são
/// responsabilidade do <c>database-architect</c> na Wave de migrations da Fase 4.
/// </summary>
public class SolicitacaoVinculoConfiguration : IEntityTypeConfiguration<SolicitacaoVinculo>
{
    public void Configure(EntityTypeBuilder<SolicitacaoVinculo> builder)
    {
        builder.ToTable("solicitacoes_vinculo");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(s => s.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(s => s.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Mensagem).HasColumnName("mensagem").HasMaxLength(1000);
        builder.Property(s => s.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(s => s.RespondidaEm).HasColumnName("respondida_em");
        builder.Property(s => s.RespondidaPorUsuarioId).HasColumnName("respondida_por_usuario_id");
        builder.Property(s => s.MotivoRecusa).HasColumnName("motivo_recusa").HasMaxLength(500);

        builder.HasIndex(s => new { s.ProfissionalUsuarioId, s.Status })
            .HasDatabaseName("ix_solicitacoes_vinculo_profissional_status");

        builder.HasIndex(s => new { s.EstabelecimentoId, s.Status, s.CriadaEm })
            .HasDatabaseName("ix_solicitacoes_vinculo_estab_status_data");

        // Unique parcial (status='Pendente'): impede duas solicitações abertas no mesmo par.
        // EF não tem builder direto para o filtro — usar HasFilter literal SQL (Postgres).
        builder.HasIndex(s => new { s.ProfissionalUsuarioId, s.EstabelecimentoId })
            .IsUnique()
            .HasFilter("status = 'Pendente'")
            .HasDatabaseName("uq_solicitacoes_vinculo_pendente");

        builder.Ignore(s => s.DomainEvents);
    }
}
