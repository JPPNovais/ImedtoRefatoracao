using System.Collections.Generic;

namespace Imedto.Backend.EtlValidator.Mapeamento;

/// <summary>
/// Mapeamento das tabelas legado → novo a serem validadas pós-ETL.
/// Subset prioritário (top 20 + tabelas com FK explícita) extraído de Docs/ETL_MAPEAMENTO.md.
/// </summary>
public static class TabelasLegadoNovo
{
    public sealed record Par(
        string Legado,
        string Novo,
        bool Descartada,
        string Observacao);

    /// <summary>
    /// Pares (tabela_legada, tabela_nova). Quando <see cref="Par.Descartada"/> = true,
    /// a expectativa é "novo possui 0 linhas vindas dessa origem" — apenas auditoria.
    /// Tabelas catálogo (seedadas) não estão aqui — não vêm do legado.
    /// </summary>
    public static readonly IReadOnlyList<Par> Pares = new List<Par>
    {
        // 1. Identidade e tenant
        new("usuarios", "usuarios", false, "Filtro: last_sign_in_at > now() - 24m. Senhas não migram."),
        new("estabelecimentos", "estabelecimentos", false, "Direct copy."),
        new("unidades_estabelecimento", "unidades_estabelecimento", false, "Direct copy."),

        // 2. Profissional + vínculos
        new("profissionais", "profissionais", false, "Apenas profissionais com usuario_id no legado."),
        new("vinculo_profissional_estabelecimento", "vinculo_profissional_estabelecimento", false, "Status ativo→Ativo, inativo→Inativo."),
        new("solicitacao_vinculo_profissional_estabelecimento", "solicitacoes_vinculo", false, "Filtro: apenas status='pendente'."),
        new("modelo_permissao_estabelecimento", "modelo_permissao_estabelecimento", false, "Split de permissoes — revisão humana."),

        // 3. Pacientes
        new("pacientes", "pacientes", false, "Direct copy multi-tenant."),

        // 4. Prontuário
        new("modelo_de_prontuario", "modelo_de_prontuario", false, "Direct copy."),
        new("prontuario_variaveis_pool", "prontuario_variaveis_pool", false, "Direct copy."),
        new("prontuarios", "prontuarios", false, "Direct copy."),
        new("evolucao_prontuario", "prontuario_evolucoes", false, "Rename + template_snapshot."),
        new("exame_fisico", "exame_fisico", false, "Direct copy + mapping evolucao_prontuario_id."),

        // 5. Agenda
        new("evento_de_agendamento", "agendamentos", false, "Rename + status enum."),

        // 6. Salas
        new("sala_atendimento", "sala_atendimento", false, "Direct copy."),

        // 7. Estoque
        new("estoque_produto", "itens_inventario", false, "Catálogos colapsados em colunas planas."),
        new("movimento_estoque", "movimentacoes_estoque", false, "Direct copy."),

        // 8. Orçamento
        new("orcamentos", "orcamentos", false, "Direct copy."),
        new("orcamento_cirurgias", "orcamento_cirurgias", false, "Direct copy."),
        new("orcamento_internacao", "orcamento_internacao", false, "Direct copy."),
        new("orcamento_anestesia", "orcamento_anestesia", false, "Direct copy."),
        new("orcamento_implante", "orcamento_implantes", false, "Pluralização."),
        new("orcamento_formas_pagamento", "orcamento_formas_pagamento", false, "Direct copy."),

        // 9. Receitas
        new("receitas", "receitas", false, "Mapeamento de tipo + tipo_notificacao → enum único."),
        new("receita_itens", "receita_itens", false, "Direct copy."),
        new("receitas_configuracao_estabelecimento", "receitas_configuracao_estabelecimento", false, "Direct copy."),
        new("medicamentos_favoritos", "medicamentos_favoritos", false, "Direct copy."),

        // 10. Financeiro
        new("financeiro_categoria", "categorias_financeiras", false, "Rename."),
        new("financeiro_forma_pagamento", "formas_pagamento", false, "Rename."),
        new("financeiro_transacao", "lancamentos", false, "Rename + direct copy."),

        // 11/12. Notificações + IA
        new("notifications", "notificacoes", false, "Filtro: últimos 90 dias."),

        // 13. Subscription
        new("assinaturas", "assinaturas", false, "Mapping de plano_id."),

        // Tabelas descartadas — esperam-se 0 linhas migradas no destino
        new("appointment_checklists", "", true, "Feature descartada."),
        new("paciente_estabelecimento", "", true, "Já dropada no legado."),
        new("orcamento_extras", "", true, "Feature descartada."),
        new("estoque_categoria", "", true, "Catálogo colapsado em itens_inventario."),
        new("estoque_fabricante", "", true, "Catálogo colapsado em itens_inventario."),
        new("estoque_fornecedor", "", true, "Catálogo colapsado em itens_inventario."),
        new("estoque_tipo_produto", "", true, "Catálogo colapsado em itens_inventario."),
        new("lgpd_access_log", "", true, "Audit forward-only — não migra."),
    };

    /// <summary>
    /// Verificações de integridade referencial no destino. Cada item vira um SELECT COUNT.
    /// Convenção: a query deve retornar 0 quando saudável.
    /// </summary>
    public static readonly IReadOnlyList<VerificacaoIntegridade> Integridade = new List<VerificacaoIntegridade>
    {
        new("Receitas órfãs (prontuario_id inválido)",
            @"SELECT COUNT(*) FROM receitas r
              WHERE r.prontuario_id IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM prontuarios p WHERE p.id = r.prontuario_id)"),

        new("Itens de receita órfãos",
            @"SELECT COUNT(*) FROM receita_itens ri
              WHERE NOT EXISTS (SELECT 1 FROM receitas r WHERE r.id = ri.receita_id)"),

        new("Agendamentos sem paciente válido",
            @"SELECT COUNT(*) FROM agendamentos a
              WHERE a.paciente_id IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM pacientes p WHERE p.id = a.paciente_id)"),

        new("Agendamentos sem profissional válido",
            @"SELECT COUNT(*) FROM agendamentos a
              WHERE NOT EXISTS (SELECT 1 FROM profissionais pr WHERE pr.usuario_id = a.profissional_usuario_id)"),

        new("Agendamentos sem estabelecimento válido",
            @"SELECT COUNT(*) FROM agendamentos a
              WHERE NOT EXISTS (SELECT 1 FROM estabelecimentos e WHERE e.id = a.estabelecimento_id)"),

        new("Prontuários sem paciente",
            @"SELECT COUNT(*) FROM prontuarios p
              WHERE NOT EXISTS (SELECT 1 FROM pacientes pa WHERE pa.id = p.paciente_id)"),

        new("Evoluções de prontuário órfãs",
            @"SELECT COUNT(*) FROM prontuario_evolucoes pe
              WHERE NOT EXISTS (SELECT 1 FROM prontuarios p WHERE p.id = pe.prontuario_id)"),

        new("Movimentações de estoque sem item",
            @"SELECT COUNT(*) FROM movimentacoes_estoque m
              WHERE NOT EXISTS (SELECT 1 FROM itens_inventario i WHERE i.id = m.item_inventario_id)"),

        new("Vínculos sem usuário em auth.users",
            @"SELECT COUNT(*) FROM vinculo_profissional_estabelecimento v
              JOIN profissionais p ON p.usuario_id = v.profissional_id
              WHERE NOT EXISTS (SELECT 1 FROM auth.users u WHERE u.id = p.usuario_id)"),

        new("Pacientes sem estabelecimento",
            @"SELECT COUNT(*) FROM pacientes p
              WHERE NOT EXISTS (SELECT 1 FROM estabelecimentos e WHERE e.id = p.estabelecimento_id)"),

        new("Lançamentos financeiros sem estabelecimento",
            @"SELECT COUNT(*) FROM lancamentos l
              WHERE NOT EXISTS (SELECT 1 FROM estabelecimentos e WHERE e.id = l.estabelecimento_id)"),

        new("Orçamentos sem paciente",
            @"SELECT COUNT(*) FROM orcamentos o
              WHERE o.paciente_id IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM pacientes p WHERE p.id = o.paciente_id)"),

        new("Assinaturas sem plano",
            @"SELECT COUNT(*) FROM assinaturas a
              WHERE NOT EXISTS (SELECT 1 FROM planos p WHERE p.id = a.plano_id)"),
    };

    public sealed record VerificacaoIntegridade(string Descricao, string SqlContagem);
}
