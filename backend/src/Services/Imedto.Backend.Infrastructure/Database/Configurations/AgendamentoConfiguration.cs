using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Salas;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        builder.ToTable("agendamentos");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(a => a.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(a => a.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(a => a.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(a => a.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(a => a.InicioPrevisto).HasColumnName("inicio_previsto").IsRequired();
        builder.Property(a => a.FimPrevisto).HasColumnName("fim_previsto").IsRequired();
        builder.Property(a => a.TipoServico).HasColumnName("tipo_servico").HasMaxLength(100).IsRequired();
        builder.Property(a => a.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(a => a.MotivoCancelamento).HasColumnName("motivo_cancelamento").HasMaxLength(500);
        builder.Property(a => a.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(a => a.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(a => a.CheckInEm).HasColumnName("check_in_em");
        builder.Property(a => a.LembretePorEmailEnviado).HasColumnName("lembrete_por_email_enviado")
            .IsRequired().HasDefaultValue(false);
        builder.Property(a => a.SalaId).HasColumnName("sala_id");

        builder.HasIndex(a => new { a.EstabelecimentoId, a.InicioPrevisto })
            .HasDatabaseName("ix_agendamentos_estab_inicio");
        builder.HasIndex(a => new { a.ProfissionalUsuarioId, a.InicioPrevisto })
            .HasDatabaseName("ix_agendamentos_prof_inicio");
        builder.HasIndex(a => new { a.PacienteId, a.InicioPrevisto })
            .HasDatabaseName("ix_agendamentos_paciente_inicio");
        builder.HasIndex(a => a.SalaId).HasDatabaseName("ix_agendamentos_sala");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(a => a.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_agendamento_estabelecimento");

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(a => a.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_agendamento_paciente");

        builder.HasOne<Sala>()
            .WithMany()
            .HasForeignKey(a => a.SalaId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_agendamento_sala");
    }
}
