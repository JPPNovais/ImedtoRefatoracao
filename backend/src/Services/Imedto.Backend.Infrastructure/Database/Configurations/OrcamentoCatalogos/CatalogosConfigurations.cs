using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;

namespace Imedto.Backend.Infrastructure.Database.Configurations.OrcamentoCatalogos;

public class CatalogoCirurgiaConfiguration : IEntityTypeConfiguration<CatalogoCirurgia>
{
    public void Configure(EntityTypeBuilder<CatalogoCirurgia> b)
    {
        b.ToTable("orcamento_catalogo_cirurgia");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
        b.Property(x => x.ValorBase).HasColumnName("valor_base").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.DuracaoPadraoMinutos).HasColumnName("duracao_padrao_minutos");
        b.Property(x => x.CodigoInterno).HasColumnName("codigo_interno").HasMaxLength(40);
        b.Property(x => x.CodigoTuss).HasColumnName("codigo_tuss").HasMaxLength(20);
        b.Property(x => x.Categoria).HasColumnName("categoria").HasMaxLength(80);
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo })
            .HasDatabaseName("ix_catalogo_cirurgia_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_catalogo_cirurgia_estabelecimento");
    }
}

public class ValorProfissionalOrcamentoConfiguration : IEntityTypeConfiguration<ValorProfissionalOrcamento>
{
    public void Configure(EntityTypeBuilder<ValorProfissionalOrcamento> b)
    {
        b.ToTable("orcamento_valor_profissional");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id");
        b.Property(x => x.Funcao).HasColumnName("funcao").HasMaxLength(60).IsRequired();
        b.Property(x => x.TempoBaseMinutos).HasColumnName("tempo_base_minutos").IsRequired();
        b.Property(x => x.ValorTempoBase).HasColumnName("valor_tempo_base").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.TempoAdicionalMinutos).HasColumnName("tempo_adicional_minutos").IsRequired();
        b.Property(x => x.ValorAdicional).HasColumnName("valor_adicional").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.ValorPlus).HasColumnName("valor_plus").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.ProfissionalUsuarioId, x.Funcao })
            .HasDatabaseName("ix_valor_prof_orc_estab_prof_funcao");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_valor_prof_orc_estabelecimento");
    }
}

public class ConfiguracaoLocalCirurgiaConfiguration : IEntityTypeConfiguration<ConfiguracaoLocalCirurgia>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoLocalCirurgia> b)
    {
        b.ToTable("orcamento_configuracao_local_cirurgia");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.TipoInternacao).HasColumnName("tipo_internacao").HasMaxLength(20).IsRequired().HasConversion<string>();
        b.Property(x => x.TempoBaseMinutos).HasColumnName("tempo_base_minutos").IsRequired();
        b.Property(x => x.ValorBase).HasColumnName("valor_base").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.TempoAdicionalMinutos).HasColumnName("tempo_adicional_minutos").IsRequired();
        b.Property(x => x.ValorAdicional).HasColumnName("valor_adicional").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.TipoInternacao }).IsUnique().HasDatabaseName("uq_config_local_estab_tipo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_config_local_cirurgia_estabelecimento");
    }
}

public class CatalogoEquipeEspecializadaConfiguration : IEntityTypeConfiguration<CatalogoEquipeEspecializada>
{
    public void Configure(EntityTypeBuilder<CatalogoEquipeEspecializada> b)
    {
        b.ToTable("orcamento_catalogo_equipe");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
        b.Property(x => x.ValorPadrao).HasColumnName("valor_padrao").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo }).HasDatabaseName("ix_catalogo_equipe_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_catalogo_equipe_estabelecimento");
    }
}

public class CatalogoImplanteConfiguration : IEntityTypeConfiguration<CatalogoImplante>
{
    public void Configure(EntityTypeBuilder<CatalogoImplante> b)
    {
        b.ToTable("orcamento_catalogo_implante");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.ItemInventarioId).HasColumnName("item_inventario_id");
        b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(200).IsRequired();
        b.Property(x => x.CustoUnitario).HasColumnName("custo_unitario").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo }).HasDatabaseName("ix_catalogo_implante_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_catalogo_implante_estabelecimento");
        b.HasOne<Domain.Inventario.ItemInventario>()
            .WithMany().HasForeignKey(x => x.ItemInventarioId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_catalogo_implante_item_inventario");
    }
}

public class CatalogoProdutoConfiguration : IEntityTypeConfiguration<CatalogoProduto>
{
    public void Configure(EntityTypeBuilder<CatalogoProduto> b)
    {
        b.ToTable("orcamento_catalogo_produto");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
        b.Property(x => x.ValorReferencia).HasColumnName("valor_referencia").HasPrecision(12, 2);
        b.Property(x => x.UsoUnico).HasColumnName("uso_unico").IsRequired().HasDefaultValue(false);
        b.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired()
            .HasConversion<string>().HasDefaultValue(TipoOrcamentoProduto.Outros);
        b.Property(x => x.Marca).HasColumnName("marca").HasMaxLength(120);
        b.Property(x => x.Unidade).HasColumnName("unidade").HasMaxLength(20).IsRequired().HasDefaultValue("un");
        b.Property(x => x.FornecedorNome).HasColumnName("fornecedor_nome").HasMaxLength(200);
        b.Property(x => x.CodigoSku).HasColumnName("codigo_sku").HasMaxLength(40);
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo }).HasDatabaseName("ix_catalogo_produto_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_catalogo_produto_estabelecimento");
    }
}

public class CatalogoCirurgiaProdutoConfiguration : IEntityTypeConfiguration<CatalogoCirurgiaProduto>
{
    public void Configure(EntityTypeBuilder<CatalogoCirurgiaProduto> b)
    {
        b.ToTable("orcamento_catalogo_cirurgia_produto");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.CatalogoCirurgiaId).HasColumnName("catalogo_cirurgia_id").IsRequired();
        b.Property(x => x.CatalogoProdutoId).HasColumnName("catalogo_produto_id").IsRequired();
        b.Property(x => x.QuantidadePadrao).HasColumnName("quantidade_padrao").HasPrecision(10, 3).IsRequired();
        b.Property(x => x.Obrigatorio).HasColumnName("obrigatorio").IsRequired().HasDefaultValue(false);
        b.Property(x => x.Incluido).HasColumnName("incluido").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.CatalogoCirurgiaId, x.CatalogoProdutoId }).IsUnique().HasDatabaseName("uq_catalogo_cirurgia_produto");

        b.HasOne<CatalogoCirurgia>()
            .WithMany().HasForeignKey(x => x.CatalogoCirurgiaId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_cirurgia_produto_cirurgia");

        b.HasOne<CatalogoProduto>()
            .WithMany().HasForeignKey(x => x.CatalogoProdutoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_cirurgia_produto_produto");
    }
}

public class ConfiguracaoPagamentoCatalogoConfiguration : IEntityTypeConfiguration<ConfiguracaoPagamentoCatalogo>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoPagamentoCatalogo> b)
    {
        b.ToTable("orcamento_configuracao_pagamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.FormaPagamentoId).HasColumnName("forma_pagamento_id").IsRequired();
        b.Property(x => x.AcrescimoPercentual).HasColumnName("acrescimo_percentual").HasPrecision(5, 2).IsRequired();
        b.Property(x => x.EntradaPercentualPadrao).HasColumnName("entrada_percentual_padrao").HasPrecision(5, 2).IsRequired();
        b.Property(x => x.TaxaParcela).HasColumnName("taxa_parcela").HasPrecision(7, 4).IsRequired();
        b.Property(x => x.ParcelasMaximas).HasColumnName("parcelas_maximas").IsRequired();
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.FormaPagamentoId }).IsUnique().HasDatabaseName("uq_config_pgto_estab_forma");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_config_pgto_estabelecimento");
        b.HasOne<Domain.Financeiro.FormaPagamento>()
            .WithMany().HasForeignKey(x => x.FormaPagamentoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_config_pgto_forma_pagamento");
    }
}

public class OrcamentoTeamRoleConfiguration : IEntityTypeConfiguration<OrcamentoTeamRole>
{
    public void Configure(EntityTypeBuilder<OrcamentoTeamRole> b)
    {
        b.ToTable("orcamento_team_role");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.Papel).HasColumnName("papel").HasMaxLength(80).IsRequired();
        b.Property(x => x.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id");
        b.Property(x => x.NomePadrao).HasColumnName("nome_padrao").HasMaxLength(200);
        b.Property(x => x.TipoHonorario).HasColumnName("tipo_honorario").HasMaxLength(20).IsRequired().HasConversion<string>();
        b.Property(x => x.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.BaseCalculo).HasColumnName("base_calculo").HasMaxLength(40).IsRequired();
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo }).HasDatabaseName("ix_orcamento_team_role_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_team_role_estabelecimento");
    }
}

public class OrcamentoAnestesistaConfiguration : IEntityTypeConfiguration<OrcamentoAnestesista>
{
    public void Configure(EntityTypeBuilder<OrcamentoAnestesista> b)
    {
        b.ToTable("orcamento_anestesista");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.ProfissionalUsuarioId).HasColumnName("profissional_usuario_id");
        b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        b.Property(x => x.Crm).HasColumnName("crm").HasMaxLength(40);
        b.Property(x => x.Especialidade).HasColumnName("especialidade").HasMaxLength(120);
        b.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(40);
        b.Property(x => x.TabelaHonorarios).HasColumnName("tabela_honorarios").HasMaxLength(80);
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo }).HasDatabaseName("ix_orcamento_anestesista_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_anestesista_estabelecimento");

        b.Ignore(x => x.Faixas);
        b.HasMany<OrcamentoAnestesistaFaixa>("_faixas")
            .WithOne()
            .HasForeignKey(f => f.AnestesistaId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_anestesista_faixa_anestesista");
        b.Metadata.FindNavigation("_faixas")!.SetField("_faixas");
        b.Metadata.FindNavigation("_faixas")!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class OrcamentoAnestesistaFaixaConfiguration : IEntityTypeConfiguration<OrcamentoAnestesistaFaixa>
{
    public void Configure(EntityTypeBuilder<OrcamentoAnestesistaFaixa> b)
    {
        b.ToTable("orcamento_anestesista_faixa");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.AnestesistaId).HasColumnName("anestesista_id").IsRequired();
        b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(120).IsRequired();
        b.Property(x => x.Valor).HasColumnName("valor").HasPrecision(12, 2).IsRequired();
        b.Property(x => x.Ordem).HasColumnName("ordem").IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.AnestesistaId, x.Descricao }).IsUnique().HasDatabaseName("uq_anestesista_faixa_descricao");
    }
}

public class OrcamentoPacoteConfiguration : IEntityTypeConfiguration<OrcamentoPacote>
{
    public void Configure(EntityTypeBuilder<OrcamentoPacote> b)
    {
        b.ToTable("orcamento_pacote");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EstabelecimentoId).HasColumnName("estabelecimento_id").IsRequired();
        b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500);
        b.Property(x => x.AnestesistaId).HasColumnName("anestesista_id");
        b.Property(x => x.ValorTotalSugerido).HasColumnName("valor_total_sugerido").HasPrecision(12, 2);
        b.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);
        b.Property(x => x.CriadaEm).HasColumnName("criada_em").IsRequired();
        b.Property(x => x.AtualizadaEm).HasColumnName("atualizada_em");
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.EstabelecimentoId, x.Ativo }).HasDatabaseName("ix_orcamento_pacote_estab_ativo");

        b.HasOne<Domain.Estabelecimentos.Estabelecimento>()
            .WithMany().HasForeignKey(x => x.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_orcamento_pacote_estabelecimento");

        b.HasOne<OrcamentoAnestesista>()
            .WithMany().HasForeignKey(x => x.AnestesistaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_orcamento_pacote_anestesista");

        b.Ignore(x => x.Procedimentos);
        b.Ignore(x => x.Produtos);
        b.Ignore(x => x.TeamRoles);

        b.HasMany<OrcamentoPacoteProcedimento>("_procedimentos")
            .WithOne().HasForeignKey(p => p.PacoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_pacote_procedimento_pacote");
        b.Metadata.FindNavigation("_procedimentos")!.SetField("_procedimentos");
        b.Metadata.FindNavigation("_procedimentos")!.SetPropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany<OrcamentoPacoteProduto>("_produtos")
            .WithOne().HasForeignKey(p => p.PacoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_pacote_produto_pacote");
        b.Metadata.FindNavigation("_produtos")!.SetField("_produtos");
        b.Metadata.FindNavigation("_produtos")!.SetPropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany<OrcamentoPacoteTeamRole>("_teamRoles")
            .WithOne().HasForeignKey(p => p.PacoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_pacote_team_role_pacote");
        b.Metadata.FindNavigation("_teamRoles")!.SetField("_teamRoles");
        b.Metadata.FindNavigation("_teamRoles")!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class OrcamentoPacoteProcedimentoConfiguration : IEntityTypeConfiguration<OrcamentoPacoteProcedimento>
{
    public void Configure(EntityTypeBuilder<OrcamentoPacoteProcedimento> b)
    {
        b.ToTable("orcamento_pacote_procedimento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacoteId).HasColumnName("pacote_id").IsRequired();
        b.Property(x => x.CatalogoCirurgiaId).HasColumnName("catalogo_cirurgia_id").IsRequired();
        b.Property(x => x.Ordem).HasColumnName("ordem").IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.PacoteId, x.CatalogoCirurgiaId }).IsUnique().HasDatabaseName("uq_pacote_procedimento");

        b.HasOne<CatalogoCirurgia>()
            .WithMany().HasForeignKey(x => x.CatalogoCirurgiaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pacote_procedimento_cirurgia");
    }
}

public class OrcamentoPacoteProdutoConfiguration : IEntityTypeConfiguration<OrcamentoPacoteProduto>
{
    public void Configure(EntityTypeBuilder<OrcamentoPacoteProduto> b)
    {
        b.ToTable("orcamento_pacote_produto");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacoteId).HasColumnName("pacote_id").IsRequired();
        b.Property(x => x.CatalogoProdutoId).HasColumnName("catalogo_produto_id").IsRequired();
        b.Property(x => x.Quantidade).HasColumnName("quantidade").HasPrecision(10, 3).IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.PacoteId, x.CatalogoProdutoId }).IsUnique().HasDatabaseName("uq_pacote_produto");

        b.HasOne<CatalogoProduto>()
            .WithMany().HasForeignKey(x => x.CatalogoProdutoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pacote_produto_produto");
    }
}

public class OrcamentoPacoteTeamRoleConfiguration : IEntityTypeConfiguration<OrcamentoPacoteTeamRole>
{
    public void Configure(EntityTypeBuilder<OrcamentoPacoteTeamRole> b)
    {
        b.ToTable("orcamento_pacote_team_role");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.PacoteId).HasColumnName("pacote_id").IsRequired();
        b.Property(x => x.TeamRoleId).HasColumnName("team_role_id").IsRequired();
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.PacoteId, x.TeamRoleId }).IsUnique().HasDatabaseName("uq_pacote_team_role");

        b.HasOne<OrcamentoTeamRole>()
            .WithMany().HasForeignKey(x => x.TeamRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pacote_team_role_team_role");
    }
}
