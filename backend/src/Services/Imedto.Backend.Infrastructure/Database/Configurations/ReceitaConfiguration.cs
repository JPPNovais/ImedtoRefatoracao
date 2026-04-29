using Imedto.Backend.Domain.Receitas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imedto.Backend.Infrastructure.Database.Configurations;

public class ReceitaConfiguration : IEntityTypeConfiguration<Receita>
{
    public void Configure(EntityTypeBuilder<Receita> builder)
    {
        builder.ToTable("receitas");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(r => r.ProntuarioId).HasColumnName("prontuario_id").IsRequired();
        builder.Property(r => r.PacienteId).HasColumnName("paciente_id").IsRequired();
        builder.Property(r => r.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(r => r.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(r => r.Tipo).HasColumnName("tipo").HasMaxLength(30).HasConversion<string>().IsRequired();
        // Tipo de notificação (Portaria 344/98) — preenchido apenas em receitas Controladas.
        builder.Property(r => r.TipoNotificacao).HasColumnName("tipo_notificacao").HasMaxLength(20).HasConversion<string?>();
        // EmitidaEm é nullable: receitas em Rascunho ainda não foram emitidas.
        builder.Property(r => r.EmitidaEm).HasColumnName("emitida_em");
        builder.Property(r => r.ValidadeAte).HasColumnName("validade_ate");
        // Setado pelo aggregate em Emitir/Finalizar conforme tipo (Controlada
        // e Antibiotico exigem retenção pela farmácia — Portaria 344/98 e RDC 471/2021).
        builder.Property(r => r.RequerRetencao).HasColumnName("requer_retencao").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.Observacoes).HasColumnName("observacoes").HasMaxLength(2000);
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(r => r.CanceladaEm).HasColumnName("cancelada_em");
        builder.Property(r => r.MotivoCancelamento).HasColumnName("motivo_cancelamento").HasMaxLength(500);
        builder.Property(r => r.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(r => r.AtualizadaEm).HasColumnName("atualizada_em");
        builder.Property(r => r.DeletadoEm).HasColumnName("deletado_em");
        builder.Property(r => r.DeletadoPorUsuarioId).HasColumnName("deletado_por_usuario_id");

        // Listagem do paciente (ordem desc por data de emissão).
        builder.HasIndex(r => new { r.PacienteId, r.EmitidaEm })
            .HasDatabaseName("ix_receitas_paciente_emitida")
            .IsDescending(false, true);

        // Filtro tenant + profissional (relatório / minhas receitas emitidas).
        builder.HasIndex(r => new { r.EstabelecimentoId, r.ProfissionalUsuarioId, r.EmitidaEm })
            .HasDatabaseName("ix_receitas_estab_prof_emitida")
            .IsDescending(false, false, true);

        // Itens via aggregate root — cascade no banco (sem aggregate, item órfão é inválido).
        builder.HasMany(r => r.Itens)
            .WithOne()
            .HasForeignKey(i => i.ReceitaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.DomainEvents);
    }
}

public class ItemReceitaConfiguration : IEntityTypeConfiguration<ItemReceita>
{
    public void Configure(EntityTypeBuilder<ItemReceita> builder)
    {
        builder.ToTable("receita_itens");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(i => i.ReceitaId).HasColumnName("receita_id").IsRequired();
        builder.Property(i => i.Ordem).HasColumnName("ordem").IsRequired();
        builder.Property(i => i.Medicamento).HasColumnName("medicamento").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Posologia).HasColumnName("posologia").HasMaxLength(500).IsRequired();
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasMaxLength(80);
        builder.Property(i => i.Via).HasColumnName("via_administracao").HasMaxLength(40).HasConversion<string?>();
        builder.Property(i => i.Observacao).HasColumnName("observacao").HasMaxLength(500);
        // Campos clínicos adicionais (paridade com legado).
        builder.Property(i => i.Concentracao).HasColumnName("concentracao").HasMaxLength(100);
        builder.Property(i => i.FormaFarmaceutica).HasColumnName("forma_farmaceutica").HasMaxLength(60);
        builder.Property(i => i.Duracao).HasColumnName("duracao").HasMaxLength(80);

        builder.HasIndex(i => new { i.ReceitaId, i.Ordem })
            .HasDatabaseName("ix_receita_itens_receita_ordem");

        builder.Ignore(i => i.DomainEvents);
    }
}

public class ConfiguracaoReceitaEstabelecimentoConfiguration
    : IEntityTypeConfiguration<ConfiguracaoReceitaEstabelecimento>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoReceitaEstabelecimento> builder)
    {
        builder.ToTable("receitas_configuracao_estabelecimento");

        // PK = EstabelecimentoId — relação 1:1.
        builder.HasKey(c => c.EstabelecimentoId);
        builder.Property(c => c.EstabelecimentoId).HasColumnName("estabelecimento_id").ValueGeneratedNever();

        builder.Property(c => c.CabecalhoHtml).HasColumnName("cabecalho_html").HasColumnType("text");
        builder.Property(c => c.RodapeHtml).HasColumnName("rodape_html").HasColumnType("text");
        builder.Property(c => c.ModeloPadraoId).HasColumnName("modelo_padrao_id");
        builder.Property(c => c.EmissorPadrao).HasColumnName("emissor_padrao").HasMaxLength(80);
        builder.Property(c => c.AtualizadaEm).HasColumnName("atualizada_em");
    }
}

public class MedicamentoFavoritoConfiguration : IEntityTypeConfiguration<MedicamentoFavorito>
{
    public void Configure(EntityTypeBuilder<MedicamentoFavorito> builder)
    {
        builder.ToTable("medicamentos_favoritos");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(f => f.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id").IsRequired();
        builder.Property(f => f.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        builder.Property(f => f.Medicamento).HasColumnName("medicamento").HasMaxLength(200).IsRequired();
        builder.Property(f => f.Posologia).HasColumnName("posologia").HasMaxLength(500);
        builder.Property(f => f.ViaAdministracao).HasColumnName("via_administracao").HasMaxLength(40).HasConversion<string?>();
        builder.Property(f => f.UsoCount).HasColumnName("uso_count").IsRequired().HasDefaultValue(0);
        builder.Property(f => f.UltimoUso).HasColumnName("ultimo_uso");
        builder.Property(f => f.CriadoEm).HasColumnName("criado_em").IsRequired();

        // Unicidade por (profissional, estabelecimento, medicamento, posologia).
        // Posologia null colide com null no Postgres por padrão → a unique trata
        // ambas as variantes (com e sem posologia) como entradas distintas.
        builder.HasIndex(f => new { f.ProfissionalUsuarioId, f.EstabelecimentoId, f.Medicamento, f.Posologia })
            .IsUnique()
            .HasDatabaseName("uq_medicamentos_favoritos_chave");

        // Ranking — ordem default da listagem.
        builder.HasIndex(f => new { f.ProfissionalUsuarioId, f.EstabelecimentoId, f.UsoCount })
            .HasDatabaseName("ix_medicamentos_favoritos_ranking")
            .IsDescending(false, false, true);

        builder.Ignore(f => f.DomainEvents);
    }
}
