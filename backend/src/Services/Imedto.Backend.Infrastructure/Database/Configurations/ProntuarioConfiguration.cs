using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Prontuarios;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProntuarioConfiguration : IEntityTypeConfiguration<Prontuario>
{
    public void Configure(EntityTypeBuilder<Prontuario> builder)
    {
        builder.ToTable("prontuarios");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(p => p.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(p => p.ModeloDeProntuarioId).HasColumnName("modelo_de_prontuario_id").IsRequired();
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(p => p.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        // 1 prontuário por paciente × estabelecimento (invariante forte).
        builder.HasIndex(p => new { p.PacienteId, p.EstabelecimentoId })
            .IsUnique()
            .HasDatabaseName("uq_prontuario_paciente_estabelecimento");

        builder.Ignore(p => p.DomainEvents);
    }
}

public class ProntuarioEvolucaoConfiguration : IEntityTypeConfiguration<ProntuarioEvolucao>
{
    public void Configure(EntityTypeBuilder<ProntuarioEvolucao> builder)
    {
        builder.ToTable("prontuario_evolucoes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.ProntuarioId).HasColumnName("prontuario_id").IsRequired();
        builder.Property(e => e.AutorUsuarioId).HasColumnName("autor_usuario_id").IsRequired();
        builder.Property(e => e.ConteudoJson).HasColumnName("conteudo").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.ModeloSnapshotJson).HasColumnName("modelo_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.ModeloDeProntuarioIdOrigem).HasColumnName("modelo_de_prontuario_id_origem").IsRequired();
        builder.Property(e => e.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(e => e.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(e => e.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        builder.HasIndex(e => new { e.ProntuarioId, e.CriadaEm })
            .HasDatabaseName("ix_evolucoes_prontuario_data");

        builder.Ignore(e => e.DomainEvents);
    }
}
