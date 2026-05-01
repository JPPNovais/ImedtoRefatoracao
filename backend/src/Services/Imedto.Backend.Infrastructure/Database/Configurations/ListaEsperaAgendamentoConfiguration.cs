using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Agendamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ListaEsperaAgendamentoConfiguration : IEntityTypeConfiguration<ListaEsperaAgendamento>
{
    public void Configure(EntityTypeBuilder<ListaEsperaAgendamento> b)
    {
        b.ToTable("lista_espera_agendamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.PacienteId).HasColumnName("paciente_id").IsRequired();
        b.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(200).IsRequired();
        b.Property(x => x.ProfissionalPreferidoId).HasColumnName("profissional_preferido_id");
        b.Property(x => x.Prioridade).HasColumnName("prioridade").HasMaxLength(20).IsRequired()
            .HasConversion<string>().HasDefaultValue(ListaEsperaPrioridade.Rotina);
        b.Property(x => x.PreferenciaPeriodo).HasColumnName("preferencia_periodo").HasMaxLength(20).IsRequired()
            .HasConversion<string>().HasDefaultValue(ListaEsperaPreferenciaPeriodo.Qualquer);
        b.Property(x => x.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        b.Property(x => x.CriadoEm).HasColumnName("criado_em").IsRequired();
        b.Property(x => x.AtendidoEm).HasColumnName("atendido_em");
        b.Property(x => x.AtendidoPorAgendamentoId).HasColumnName("atendido_por_agendamento_id");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.AtendidoEm })
            .HasDatabaseName("ix_lista_espera_estab_atendido");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_lista_espera_estabelecimento");

        b.HasOne<Domain.Pacientes.Paciente>()
            .WithMany().HasForeignKey(x => x.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lista_espera_paciente");

        b.HasOne<Agendamento>()
            .WithMany().HasForeignKey(x => x.AtendidoPorAgendamentoId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_lista_espera_agendamento");
    }
}
