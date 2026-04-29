using Imedto.Backend.Domain.Prontuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ExameFisicoConfiguration : IEntityTypeConfiguration<ExameFisico>
{
    public void Configure(EntityTypeBuilder<ExameFisico> builder)
    {
        builder.ToTable("exame_fisico");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.EvolucaoId).HasColumnName("evolucao_id").IsRequired();
        builder.Property(e => e.ProntuarioId).HasColumnName("prontuario_id").IsRequired();
        builder.Property(e => e.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(e => e.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(e => e.RealizadoEm).HasColumnName("realizado_em").IsRequired();
        builder.Property(e => e.RealizadoPorUsuarioId).HasColumnName("realizado_por_usuario_id").IsRequired();
        builder.Property(e => e.DadosGeraisJson).HasColumnName("dados_gerais_json").HasColumnType("jsonb");
        builder.Property(e => e.ObservacoesGerais).HasColumnName("observacoes_gerais").HasMaxLength(2000);
        builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(e => e.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(e => e.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        builder.HasIndex(e => new { e.ProntuarioId, e.RealizadoEm })
            .HasDatabaseName("ix_exame_fisico_prontuario_realizado")
            .IsDescending(false, true);
        builder.HasIndex(e => new { e.PacienteId, e.RealizadoEm })
            .HasDatabaseName("ix_exame_fisico_paciente_realizado")
            .IsDescending(false, true);
        builder.HasIndex(e => e.EvolucaoId).HasDatabaseName("ix_exame_fisico_evolucao");
        builder.HasIndex(e => e.EstabelecimentoId).HasDatabaseName("ix_exame_fisico_estabelecimento");

        // Relação 1-N com Regioes (cascade no banco — child sem aggregate root é inválido).
        builder.HasMany(e => e.Regioes)
            .WithOne()
            .HasForeignKey(r => r.ExameFisicoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Backing field — coleção exposta via IReadOnlyCollection.
        builder.Metadata.FindNavigation(nameof(ExameFisico.Regioes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(e => e.DomainEvents);
    }
}

public class RegiaoExameFisicoConfiguration : IEntityTypeConfiguration<RegiaoExameFisico>
{
    public void Configure(EntityTypeBuilder<RegiaoExameFisico> builder)
    {
        builder.ToTable("exame_fisico_regioes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(r => r.ExameFisicoId).HasColumnName("exame_fisico_id").IsRequired();
        builder.Property(r => r.RegiaoCodigo).HasColumnName("regiao_codigo").IsRequired().HasMaxLength(60);
        builder.Property(r => r.RegiaoPaiCodigo).HasColumnName("regiao_pai_codigo").HasMaxLength(60);
        builder.Property(r => r.Lateralidade).HasColumnName("lateralidade").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(r => r.Achados).HasColumnName("achados").HasMaxLength(2000);
        builder.Property(r => r.Severidade).HasColumnName("severidade").HasMaxLength(20).HasConversion<string?>();
        builder.Property(r => r.Ordem).HasColumnName("ordem").IsRequired().HasDefaultValue(0);

        builder.HasIndex(r => new { r.ExameFisicoId, r.RegiaoCodigo })
            .IsUnique()
            .HasDatabaseName("ux_exame_fisico_regiao_codigo");

        builder.Ignore(r => r.DomainEvents);
    }
}
