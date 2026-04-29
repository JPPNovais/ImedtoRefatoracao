using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Imedto.Backend.Domain.Cirurgias;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ProcedimentoCirurgicoConfiguration : IEntityTypeConfiguration<ProcedimentoCirurgico>
{
    public void Configure(EntityTypeBuilder<ProcedimentoCirurgico> builder)
    {
        builder.ToTable("procedimentos_cirurgicos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(p => p.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(p => p.ProntuarioId).HasColumnName("prontuario_id").IsRequired();
        builder.Property(p => p.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(p => p.AgendamentoId).HasColumnName("agendamento_id");
        builder.Property(p => p.DataAgendada).HasColumnName("data_agendada");
        builder.Property(p => p.DataRealizada).HasColumnName("data_realizada");
        builder.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(p => p.CirurgiaPrincipal).HasColumnName("cirurgia_principal").HasMaxLength(200).IsRequired();
        builder.Property(p => p.CirurgiaCodigo).HasColumnName("cirurgia_codigo").HasMaxLength(40);
        builder.Property(p => p.DescricaoCirurgica).HasColumnName("descricao_cirurgica");
        builder.Property(p => p.FichaAnestesica)
            .HasColumnName("ficha_anestesica")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<FichaAnestesica>(v, (JsonSerializerOptions?)null));
        builder.Property(p => p.EvolucaoPosOp).HasColumnName("evolucao_pos_op");
        builder.Property(p => p.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);
        builder.Property(p => p.CanceladoEm).HasColumnName("cancelado_em");
        builder.Property(p => p.MotivoCancelamento).HasColumnName("motivo_cancelamento").HasMaxLength(500);
        builder.Property(p => p.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(p => p.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Ignore(p => p.DomainEvents);

        builder.HasMany(p => p.Equipe)
            .WithOne()
            .HasForeignKey(m => m.ProcedimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_membro_equipe_cirurgica_procedimento");

        builder.HasIndex(p => new { p.EstabelecimentoId, p.DataAgendada })
            .HasDatabaseName("ix_procedimento_estab_data_agendada");
        builder.HasIndex(p => new { p.PacienteId, p.DataRealizada })
            .HasDatabaseName("ix_procedimento_paciente_data_realizada")
            .IsDescending(false, true);

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(p => p.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_procedimento_estabelecimento");
        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(p => p.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_procedimento_paciente");
        builder.HasOne<Domain.Prontuarios.Prontuario>()
            .WithMany()
            .HasForeignKey(p => p.ProntuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_procedimento_prontuario");
        builder.HasOne<Domain.Agendamentos.Agendamento>()
            .WithMany()
            .HasForeignKey(p => p.AgendamentoId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_procedimento_agendamento");
    }
}

public class MembroEquipeCirurgicaConfiguration : IEntityTypeConfiguration<MembroEquipeCirurgica>
{
    public void Configure(EntityTypeBuilder<MembroEquipeCirurgica> builder)
    {
        builder.ToTable("equipe_cirurgica");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(m => m.ProcedimentoId).HasColumnName("procedimento_id").IsRequired();
        builder.Property(m => m.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(m => m.Papel).HasColumnName("papel").HasMaxLength(40).IsRequired()
            .HasConversion<string>();
        builder.Property(m => m.Ordem).HasColumnName("ordem").IsRequired();

        builder.Ignore(m => m.DomainEvents);

        builder.HasIndex(m => new { m.ProcedimentoId, m.Papel })
            .HasDatabaseName("ix_equipe_cirurgica_procedimento_papel");
        builder.HasIndex(m => new { m.ProcedimentoId, m.ProfissionalUsuarioId, m.Papel })
            .IsUnique()
            .HasDatabaseName("uq_equipe_cirurgica_procedimento_profissional_papel");
    }
}
