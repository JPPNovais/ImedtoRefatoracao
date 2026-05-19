using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamentos");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(o => o.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(o => o.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(o => o.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
        builder.Property(o => o.Titulo).HasColumnName("titulo").HasMaxLength(120);
        builder.Property(o => o.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(o => o.Validade).HasColumnName("validade").IsRequired();
        builder.Property(o => o.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(o => o.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(o => o.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(o => o.AtualizadoEm).HasColumnName("atualizado_em");

        builder.Property(o => o.ProcedimentoCirurgicoId).HasColumnName("procedimento_cirurgico_id");
        builder.Property(o => o.AgendamentoId).HasColumnName("agendamento_id");
        builder.Property(o => o.CustoImplantesTotal).HasColumnName("custo_implantes_total")
            .HasPrecision(12, 2).IsRequired().HasDefaultValue(0m);

        // Local cirúrgico embutido (substitui o antigo orcamento_internacao).
        builder.Property(o => o.TipoLocal).HasColumnName("tipo_local")
            .HasMaxLength(20).HasConversion<string>();
        builder.Property(o => o.TempoLocalMinutos).HasColumnName("tempo_local_minutos");
        builder.Property(o => o.ValorLocal).HasColumnName("valor_local")
            .HasPrecision(12, 2).IsRequired().HasDefaultValue(0m);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);

        builder.HasMany(o => o.Itens)
            .WithOne()
            .HasForeignKey(i => i.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_item_orcamento_orcamento");

        builder.HasMany(o => o.Equipe)
            .WithOne()
            .HasForeignKey(e => e.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_equipe_orcamento");

        builder.HasMany(o => o.Implantes)
            .WithOne()
            .HasForeignKey(i => i.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_implante_orcamento");

        builder.HasMany(o => o.FormasPagamento)
            .WithOne()
            .HasForeignKey(f => f.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_forma_pagamento_orcamento");

        builder.HasMany(o => o.Cirurgias)
            .WithOne()
            .HasForeignKey(c => c.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_cirurgia_orcamento");

        builder.HasOne(o => o.Anestesia)
            .WithOne()
            .HasForeignKey<OrcamentoAnestesia>(a => a.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_anestesia_orcamento");

        builder.HasIndex(o => new { o.EstabelecimentoId, o.Status })
            .HasDatabaseName("ix_orcamento_estab_status");
        builder.HasIndex(o => new { o.EstabelecimentoId, o.PacienteId })
            .HasDatabaseName("ix_orcamento_estab_paciente");
        builder.HasIndex(o => o.ProcedimentoCirurgicoId)
            .HasDatabaseName("ix_orcamento_procedimento_cirurgico");
        builder.HasIndex(o => o.AgendamentoId)
            .HasDatabaseName("ix_orcamento_agendamento")
            .HasFilter("agendamento_id IS NOT NULL");

        builder.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany()
            .HasForeignKey(o => o.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_orcamento_estabelecimento");

        builder.HasOne<Domain.Pacientes.Paciente>()
            .WithMany()
            .HasForeignKey(o => o.PacienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_orcamento_paciente");

        // FK opcional para o procedimento cirúrgico: SET NULL se a cirurgia for removida —
        // o orçamento continua válido (cotação histórica).
        builder.HasOne<Domain.Cirurgias.ProcedimentoCirurgico>()
            .WithMany()
            .HasForeignKey(o => o.ProcedimentoCirurgicoId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_orcamento_procedimento_cirurgico");

        builder.HasOne<Domain.Agendamentos.Agendamento>()
            .WithMany()
            .HasForeignKey(o => o.AgendamentoId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_orcamento_agendamento");
    }
}
