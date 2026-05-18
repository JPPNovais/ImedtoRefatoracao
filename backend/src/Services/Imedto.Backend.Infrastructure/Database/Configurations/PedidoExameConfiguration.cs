using System.Text.Json;
using Imedto.Backend.Domain.PedidosExame;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class PedidoExameConfiguration : IEntityTypeConfiguration<PedidoExame>
{
    public void Configure(EntityTypeBuilder<PedidoExame> builder)
    {
        builder.ToTable("pedidos_exame");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(p => p.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(p => p.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(p => p.Tipo).HasColumnName("tipo").HasMaxLength(20).HasConversion<string>().IsRequired();

        // List<string> persistida como jsonb. ValueComparer cobre detecção de
        // mudanças no array (EF default trata listas como referência → não
        // detecta append/remove sem comparer).
        var listComparer = new ValueComparer<List<string>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (acc, s) => HashCode.Combine(acc, s == null ? 0 : s.GetHashCode())),
            v => v.ToList());

        builder.Property(p => p.Exames)
            .HasColumnName("exames")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(listComparer);

        builder.Property(p => p.IndicacaoClinica).HasColumnName("indicacao_clinica").HasColumnType("text").IsRequired();
        builder.Property(p => p.Cid10).HasColumnName("cid10").HasMaxLength(8);
        builder.Property(p => p.Observacoes).HasColumnName("observacoes").HasColumnType("text");
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(p => p.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        builder.HasIndex(p => new { p.PacienteId, p.CriadoEm })
            .HasDatabaseName("ix_pedidos_exame_paciente_criado")
            .IsDescending(false, true);

        builder.HasIndex(p => new { p.EstabelecimentoId, p.CriadoEm })
            .HasDatabaseName("ix_pedidos_exame_estab_criado")
            .IsDescending(false, true);

        builder.Ignore(p => p.DomainEvents);
    }
}
