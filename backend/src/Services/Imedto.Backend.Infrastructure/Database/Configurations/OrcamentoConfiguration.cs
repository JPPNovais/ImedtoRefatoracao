using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    /// <summary>
    /// Opções de serialização do <see cref="ConfigPagamentoOrcamento"/> persistido como
    /// jsonb. PropertyNamingPolicy=null preserva PascalCase, evitando renomes implícitos
    /// — quem ler o jsonb direto no Postgres vê os mesmos nomes do POCO.
    /// </summary>
    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamentos");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(o => o.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(o => o.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(o => o.Numero).HasColumnName("numero").HasMaxLength(20).IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasMaxLength(20).IsRequired()
            .HasConversion<string>();
        builder.Property(o => o.Validade).HasColumnName("validade").IsRequired();
        builder.Property(o => o.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(o => o.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(o => o.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(o => o.AtualizadoEm).HasColumnName("atualizado_em");

        // Item 3.3.B — extensões "completo". Defaults preservam orçamento simples existente.
        // HasDefaultValue("Simples") aplica diretamente o nome da enum como default da
        // coluna VARCHAR — combinado com HasConversion<string>() para escrita/leitura.
        builder.Property(o => o.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(TipoOrcamento.Simples);
        builder.Property(o => o.ProcedimentoCirurgicoId).HasColumnName("procedimento_cirurgico_id");
        // Item 7 — config de pagamento como jsonb com schema fechado (POCO). Uso de
        // HasConversion (em vez de OwnsOne) preserva a coluna única + null sem precisar
        // de propriedades shadow espalhadas. ValueComparer compara via JSON serializado
        // para que o EF detecte mudanças mesmo quando o objeto é trocado por outro
        // logicamente equivalente.
        builder.Property(o => o.Configuracao)
            .HasColumnName("config_pagamento_json")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, ConfigJsonOptions),
                v => string.IsNullOrWhiteSpace(v)
                    ? null
                    : JsonSerializer.Deserialize<ConfigPagamentoOrcamento>(v, ConfigJsonOptions),
                new ValueComparer<ConfigPagamentoOrcamento?>(
                    (a, b) => JsonSerializer.Serialize(a, ConfigJsonOptions) == JsonSerializer.Serialize(b, ConfigJsonOptions),
                    v => v == null ? 0 : JsonSerializer.Serialize(v, ConfigJsonOptions).GetHashCode(),
                    v => v == null ? null : JsonSerializer.Deserialize<ConfigPagamentoOrcamento>(JsonSerializer.Serialize(v, ConfigJsonOptions), ConfigJsonOptions)));
        builder.Property(o => o.CustoImplantesTotal).HasColumnName("custo_implantes_total")
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

        // Item 6 — cirurgias múltiplas (N:1) + internação 1:1 + anestesia 1:1.
        builder.HasMany(o => o.Cirurgias)
            .WithOne()
            .HasForeignKey(c => c.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_cirurgia_orcamento");

        builder.HasOne(o => o.Internacao)
            .WithOne()
            .HasForeignKey<OrcamentoInternacao>(i => i.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_internacao_orcamento");

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
    }
}
