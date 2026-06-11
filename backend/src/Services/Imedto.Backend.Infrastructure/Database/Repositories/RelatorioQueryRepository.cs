using Dapper;
using Imedto.Backend.Contracts.Relatorios;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório Dapper para os relatórios consolidados. Toda query filtra por
/// <c>estabelecimento_id</c> (defense-in-depth multi-tenant) e usa parâmetros nomeados —
/// nada de string concatenation.
///
/// Os 4 métodos consolidados cobrem os 9 RPCs SQL legados (rpc_report_*):
/// - <see cref="RelatorioFinanceiro"/> → cash_flow + financial_summary + financial_by_category
/// - <see cref="RelatorioOperacionalDashboard"/>/<see cref="RelatorioOperacionalAgenda"/>/<see cref="RelatorioOperacionalInventario"/>
///   → dashboard_summary + agenda_summary + inventory_summary
/// - <see cref="RelatorioPacientes"/>/<see cref="RelatorioProfissionais"/>
///   → patients_summary + professionals_performance
/// - <see cref="RelatorioOrcamentos"/> → budgets_summary
///
/// TODO database-architect: avaliar índices abaixo na próxima Wave de migrations
/// (criar apenas se EXPLAIN ANALYZE mostrar seq scan dominante em produção):
///   - idx_lancamentos_relatorios          (estabelecimento_id, status, data_pagamento)
///     [parcialmente coberto por ix_lancamentos_estab_status; data_pagamento ajuda]
///   - idx_lancamentos_vencimento          (estabelecimento_id, status, data_vencimento)
///   - idx_agendamentos_relatorios         (estabelecimento_id, status, inicio_previsto)
///   - idx_orcamentos_relatorios           (estabelecimento_id, status, criado_em)
///   - idx_movimentacoes_estab_data        (estabelecimento_id, criado_em)
/// </summary>
public class RelatorioQueryRepository
{
    private const int TopAtivosLimite = 10;
    private const int TopMovimentacoesLimite = 10;

    private readonly string _connStr;
    private readonly ConsolidacaoFinanceiraQueryRepository _consolidacao;

    public RelatorioQueryRepository(
        AppReadConnectionString conn,
        ConsolidacaoFinanceiraQueryRepository consolidacao)
    {
        _connStr = conn.Value;
        _consolidacao = consolidacao;
    }

    // -------------------------------------------------------------------------
    // Handlers legados (mantidos para retrocompatibilidade — Wave de frontend
    // migra para os endpoints consolidados gradualmente).
    // -------------------------------------------------------------------------

    public async Task<IEnumerable<FaturamentoCategoriaDto>> RelatorioFaturamento(
        long estabelecimentoId,
        DateOnly? dataInicio,
        DateOnly? dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                categoria               AS Categoria,
                tipo                    AS Tipo,
                COALESCE(SUM(CASE WHEN status = 'Pago'     THEN valor ELSE 0 END), 0) AS TotalPago,
                COALESCE(SUM(CASE WHEN status = 'Pendente' THEN valor ELSE 0 END), 0) AS TotalPendente,
                COUNT(*)                AS Quantidade
            FROM lancamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND status != 'Cancelado'
              AND (@DataInicio::date IS NULL OR data_vencimento >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR data_vencimento <= @DataFim::date)
            GROUP BY categoria, tipo
            ORDER BY tipo, SUM(CASE WHEN status = 'Pago' THEN valor ELSE 0 END) DESC
            """;

        return await conn.QueryAsync<FaturamentoCategoriaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = ParaDateTime(dataInicio),
            DataFim = ParaDateTime(dataFim)
        });
    }

    public async Task<RelatorioAgendamentosDto> RelatorioAgendamentos(
        long estabelecimentoId,
        DateOnly? dataInicio,
        DateOnly? dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Batch único: 1 round-trip ao Postgres em vez de 2.
        const string sqlBatch = """
            SELECT status AS Status, COUNT(*) AS Quantidade
            FROM agendamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::date IS NULL OR inicio_previsto::date >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR inicio_previsto::date <= @DataFim::date)
            GROUP BY status
            ORDER BY Quantidade DESC;

            SELECT inicio_previsto::date AS Data, COUNT(*) AS Quantidade
            FROM agendamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::date IS NULL OR inicio_previsto::date >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR inicio_previsto::date <= @DataFim::date)
            GROUP BY inicio_previsto::date
            ORDER BY Data;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = ParaDateTime(dataInicio),
            DataFim = ParaDateTime(dataFim)
        };

        await using var grid = await conn.QueryMultipleAsync(sqlBatch, p);
        var porStatus = (await grid.ReadAsync<AgendamentosPorStatusDto>()).ToList();
        var porDia = (await grid.ReadAsync<AgendamentosPorDiaDto>()).ToList();

        return new RelatorioAgendamentosDto
        {
            Total = porStatus.Sum(s => s.Quantidade),
            PorStatus = porStatus,
            PorDia = porDia
        };
    }

    // -------------------------------------------------------------------------
    // 1) RELATÓRIO FINANCEIRO CONSOLIDADO
    //    Substitui rpc_report_cash_flow + rpc_report_financial_summary
    //                + rpc_report_financial_by_category.
    // -------------------------------------------------------------------------

    public async Task<RelatorioFinanceiroDto> RelatorioFinanceiro(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim,
        string agruparPor,
        bool incluirPorPaciente = false)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Totais (resumo) — sempre calculados, independente do agruparPor.
        // Usa data_pagamento para receitas/despesas efetivamente realizadas (Pago).
        const string sqlResumo = """
            SELECT
                COALESCE(SUM(CASE WHEN tipo = 'receita' THEN valor ELSE 0 END), 0) AS TotalReceitas,
                COALESCE(SUM(CASE WHEN tipo = 'despesa' THEN valor ELSE 0 END), 0) AS TotalDespesas
            FROM lancamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND status = 'Pago'
              AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        };

        var resumo = await conn.QuerySingleAsync<(decimal TotalReceitas, decimal TotalDespesas)>(sqlResumo, p);

        var breakdown = agruparPor switch
        {
            "categoria" => await BreakdownPorCategoria(conn, p),
            "forma_pagamento" => await BreakdownPorFormaPagamento(conn, p),
            _ => await BreakdownTemporal(conn, p, agruparPor) // dia/semana/mes
        };

        IList<CustoLucroPacienteDto>? porPaciente = null;
        if (incluirPorPaciente)
            porPaciente = await _consolidacao.ObterCustoLucroPorPaciente(estabelecimentoId, dataInicio, dataFim);

        return new RelatorioFinanceiroDto
        {
            TotalReceitas = resumo.TotalReceitas,
            TotalDespesas = resumo.TotalDespesas,
            Saldo = resumo.TotalReceitas - resumo.TotalDespesas,
            Breakdown = breakdown,
            PorPaciente = porPaciente
        };
    }

    private static async Task<IList<RowSummary>> BreakdownTemporal(NpgsqlConnection conn, object p, string granularidade)
    {
        // DATE_TRUNC nativo do Postgres — agrupa por dia/semana/mês mantendo o mesmo plano.
        // Saldo (receita - despesa) sai como Valor; Count = total de lançamentos no bucket.
        var truncExpr = granularidade switch
        {
            "semana" => "date_trunc('week', data_pagamento)::date",
            "mes" => "date_trunc('month', data_pagamento)::date",
            _ => "data_pagamento"
        };

        var sql = $$"""
            SELECT
                to_char({{truncExpr}}, 'YYYY-MM-DD') AS Chave,
                COALESCE(SUM(CASE WHEN tipo = 'receita' THEN valor ELSE -valor END), 0) AS Valor,
                COUNT(*) AS Count
            FROM lancamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND status = 'Pago'
              AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            GROUP BY {{truncExpr}}
            ORDER BY {{truncExpr}}
            """;

        return (await conn.QueryAsync<RowSummary>(sql, p)).ToList();
    }

    private static async Task<IList<RowSummary>> BreakdownPorCategoria(NpgsqlConnection conn, object p)
    {
        // lancamentos.categoria é texto livre — agrupamos pelo próprio valor.
        // Receitas e despesas convivem na mesma categoria? Sim, mas Tipo já está implícito:
        // o valor positivo = receita líquida do bucket, negativo = saldo de despesa pesa
        // na visão. Aqui priorizamos cada (categoria, tipo) como linha distinta.
        const string sql = """
            SELECT
                (categoria || ' (' || tipo || ')') AS Chave,
                COALESCE(SUM(valor), 0)            AS Valor,
                COUNT(*)                           AS Count
            FROM lancamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND status = 'Pago'
              AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            GROUP BY categoria, tipo
            ORDER BY SUM(valor) DESC
            """;

        return (await conn.QueryAsync<RowSummary>(sql, p)).ToList();
    }

    private static async Task<IList<RowSummary>> BreakdownPorFormaPagamento(NpgsqlConnection conn, object p)
    {
        // O modelo refatorado ainda não tem FK de forma_pagamento em lancamentos
        // (legado tinha forma_pagamento_id). Retornamos apenas as formas cadastradas,
        // todas com valor zero — front exibe "Sem dados de forma de pagamento por
        // lançamento" enquanto a coluna não é introduzida.
        // TODO database-architect: adicionar lancamentos.forma_pagamento_id quando o
        // produto formalizar o vínculo (item de roadmap financeiro).
        const string sql = """
            SELECT nome AS Chave, 0::numeric AS Valor, 0 AS Count
            FROM formas_pagamento
            WHERE estabelecimento_id = @EstabelecimentoId
              AND ativo = TRUE
            ORDER BY nome
            """;

        return (await conn.QueryAsync<RowSummary>(sql, p)).ToList();
    }

    // -------------------------------------------------------------------------
    // 2) RELATÓRIO OPERACIONAL — dashboard / agenda / inventário
    //    Substitui rpc_report_dashboard_summary + rpc_report_agenda_summary
    //                + rpc_report_inventory_summary.
    // -------------------------------------------------------------------------

    public async Task<DashboardKpisDto> RelatorioOperacionalDashboard(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Quatro queries leves em paralelo seriam um luxo desnecessário aqui — preferimos
        // 1 round-trip + uma CTE inline que monta tudo de uma vez. Mantém o plano simples.
        const string sql = """
            WITH agenda AS (
                SELECT
                    COUNT(*) AS total,
                    COUNT(*) FILTER (WHERE status = 'Concluido') AS concluidos,
                    COUNT(*) FILTER (WHERE status = 'Cancelado') AS cancelados
                FROM agendamentos
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
            ),
            financeiro AS (
                SELECT
                    COALESCE(SUM(CASE WHEN tipo = 'receita' THEN valor END), 0) AS receitas,
                    COALESCE(SUM(CASE WHEN tipo = 'despesa' THEN valor END), 0) AS despesas
                FROM lancamentos
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND status = 'Pago'
                  AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            ),
            pacientes AS (
                SELECT COUNT(*) AS novos
                FROM pacientes
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND deletado_em IS NULL
                  AND criado_em::date BETWEEN @DataInicio::date AND @DataFim::date
            )
            SELECT
                a.total                                                                 AS TotalAgendamentos,
                a.concluidos                                                            AS AgendamentosConcluidos,
                a.cancelados                                                            AS AgendamentosCancelados,
                CASE WHEN a.total > 0
                     THEN ROUND((a.concluidos::numeric / a.total) * 100, 2)
                     ELSE 0 END                                                         AS TaxaOcupacao,
                CASE WHEN a.total > 0
                     THEN ROUND((a.cancelados::numeric / a.total) * 100, 2)
                     ELSE 0 END                                                         AS TaxaCancelamento,
                f.receitas                                                              AS Faturamento,
                f.despesas                                                              AS Despesas,
                (f.receitas - f.despesas)                                               AS LucroLiquido,
                CASE WHEN a.concluidos > 0
                     THEN ROUND(f.receitas / a.concluidos, 2)
                     ELSE 0 END                                                         AS TicketMedio,
                p.novos                                                                 AS NovosPacientes
            FROM agenda a, financeiro f, pacientes p
            """;

        return await conn.QuerySingleAsync<DashboardKpisDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        });
    }

    public async Task<AgendaResumoDto> RelatorioOperacionalAgenda(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        };

        // Batch único: 1 round-trip ao Postgres em vez de 3.
        // Ordem dos SELECTs: status → profissional → dia da semana (deve bater com Read* abaixo).
        // - profissional: nome via usuarios.nome_completo; fallback para usuario_id caso apagado.
        // - dia da semana: ISO weekday 1=segunda…7=domingo (front mapeia para nome do dia).
        const string sqlBatch = """
            SELECT status AS Chave, COUNT(*)::numeric AS Valor, COUNT(*) AS Count
            FROM agendamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
            GROUP BY status
            ORDER BY Count DESC;

            SELECT
                COALESCE(u.nome_completo, 'Profissional #' || a.profissional_usuario_id::text) AS Chave,
                0::numeric                                                                     AS Valor,
                COUNT(*)                                                                       AS Count
            FROM agendamentos a
            LEFT JOIN usuarios u ON u.id = a.profissional_usuario_id
            WHERE a.estabelecimento_id = @EstabelecimentoId
              AND a.inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
            GROUP BY u.nome_completo, a.profissional_usuario_id
            ORDER BY Count DESC;

            SELECT
                EXTRACT(ISODOW FROM inicio_previsto)::int::text AS Chave,
                0::numeric                                      AS Valor,
                COUNT(*)                                        AS Count
            FROM agendamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
            GROUP BY EXTRACT(ISODOW FROM inicio_previsto)
            ORDER BY Chave;
            """;

        await using var grid = await conn.QueryMultipleAsync(sqlBatch, p);
        var porStatus = (await grid.ReadAsync<RowSummary>()).ToList();
        var porProf = (await grid.ReadAsync<RowSummary>()).ToList();
        var porDia = (await grid.ReadAsync<RowSummary>()).ToList();

        return new AgendaResumoDto
        {
            Total = porStatus.Sum(r => r.Count),
            PorStatus = porStatus,
            PorProfissional = porProf,
            PorDiaSemana = porDia
        };
    }

    public async Task<InventarioResumoDto> RelatorioOperacionalInventario(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            Limite = TopMovimentacoesLimite
        };

        // Batch único: 1 round-trip ao Postgres em vez de 2.
        // Ordem: resumo (single) → top movimentações (lista por volume absoluto, agrega
        // custo_total — útil para identificar itens de maior giro).
        const string sqlBatch = """
            SELECT
                COUNT(*)                                                            AS TotalItens,
                COUNT(*) FILTER (WHERE quantidade_atual <= quantidade_minima)        AS ItensAbaixoMinimo,
                COALESCE(SUM(quantidade_atual * custo_medio), 0)                     AS ValorTotalEstoque
            FROM itens_inventario
            WHERE estabelecimento_id = @EstabelecimentoId
              AND ativo = TRUE;

            SELECT
                i.nome                          AS Chave,
                COALESCE(SUM(m.custo_total), 0) AS Valor,
                COUNT(*)                        AS Count
            FROM movimentacoes_estoque m
            INNER JOIN itens_inventario i ON i.id = m.item_inventario_id
            WHERE m.estabelecimento_id = @EstabelecimentoId
              AND m.deletado_em IS NULL
              AND m.criado_em::date BETWEEN @DataInicio::date AND @DataFim::date
            GROUP BY i.nome
            ORDER BY Count DESC
            LIMIT @Limite;
            """;

        await using var grid = await conn.QueryMultipleAsync(sqlBatch, p);
        var resumo = await grid.ReadFirstAsync<(int TotalItens, int ItensAbaixoMinimo, decimal ValorTotalEstoque)>();
        var top = (await grid.ReadAsync<RowSummary>()).ToList();

        return new InventarioResumoDto
        {
            TotalItens = resumo.TotalItens,
            ItensAbaixoMinimo = resumo.ItensAbaixoMinimo,
            ValorTotalEstoque = resumo.ValorTotalEstoque,
            TopMovimentacoes = top
        };
    }

    // -------------------------------------------------------------------------
    // 3) RELATÓRIO DE PESSOAS — pacientes / profissionais
    //    Substitui rpc_report_patients_summary + rpc_report_professionals_performance.
    // -------------------------------------------------------------------------

    public async Task<PacientesResumoDto> RelatorioPacientes(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            Limite = TopAtivosLimite
        };

        // Batch único: 1 round-trip ao Postgres em vez de 3.
        // Ordem dos SELECTs: resumo (Novos/Retornos) → faixa etária → top ativos.
        // - Novos: pacientes criados no período. Retornos: pacientes com ≥2 agendamentos
        //   concluídos no período.
        // - Faixa etária a partir de data_nascimento (anos completos).
        // - Top 10 pacientes por nº de agendamentos. LGPD: somente id + nome
        //   (sem CPF, telefone, e-mail, data_nascimento). Tenant-scoped via estabelecimento_id.
        const string sqlBatch = """
            WITH novos AS (
                SELECT COUNT(*) AS qtd
                FROM pacientes
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND deletado_em IS NULL
                  AND criado_em::date BETWEEN @DataInicio::date AND @DataFim::date
            ),
            retornos AS (
                SELECT COUNT(*) AS qtd FROM (
                    SELECT paciente_id
                    FROM agendamentos
                    WHERE estabelecimento_id = @EstabelecimentoId
                      AND status = 'Concluido'
                      AND inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
                    GROUP BY paciente_id
                    HAVING COUNT(*) >= 2
                ) t
            )
            SELECT (SELECT qtd FROM novos) AS Novos,
                   (SELECT qtd FROM retornos) AS Retornos;

            SELECT
                CASE
                    WHEN idade IS NULL  THEN 'Sem dado'
                    WHEN idade < 18     THEN '0-17'
                    WHEN idade < 30     THEN '18-29'
                    WHEN idade < 45     THEN '30-44'
                    WHEN idade < 60     THEN '45-59'
                    ELSE '60+'
                END                              AS Chave,
                0::numeric                       AS Valor,
                COUNT(*)                         AS Count
            FROM (
                SELECT
                    CASE
                        WHEN data_nascimento IS NULL THEN NULL
                        ELSE EXTRACT(YEAR FROM AGE(CURRENT_DATE, data_nascimento))::int
                    END AS idade
                FROM pacientes
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND deletado_em IS NULL
            ) t
            GROUP BY 1
            ORDER BY 1;

            SELECT
                p.id                AS PacienteId,
                p.nome_completo     AS Nome,
                COUNT(a.id)         AS Atendimentos
            FROM pacientes p
            INNER JOIN agendamentos a
                    ON a.paciente_id = p.id
                   AND a.estabelecimento_id = p.estabelecimento_id
                   AND a.inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
            WHERE p.estabelecimento_id = @EstabelecimentoId
              AND p.deletado_em IS NULL
            GROUP BY p.id, p.nome_completo
            ORDER BY Atendimentos DESC, p.nome_completo
            LIMIT @Limite;
            """;

        await using var grid = await conn.QueryMultipleAsync(sqlBatch, p);
        var resumo = await grid.ReadFirstAsync<(int Novos, int Retornos)>();
        var faixa = (await grid.ReadAsync<RowSummary>()).ToList();
        var top = (await grid.ReadAsync<TopPacienteDto>()).ToList();

        return new PacientesResumoDto
        {
            Novos = resumo.Novos,
            Retornos = resumo.Retornos,
            PorFaixaEtaria = faixa,
            TopAtivos = top
        };
    }

    public async Task<ProfissionaisResumoDto> RelatorioProfissionais(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        };

        // Por profissional ativo no estabelecimento: contagem de agendamentos no período,
        // concluídos e faturamento associado (via lancamentos com criado_por_usuario_id =
        // profissional, tipo receita, status pago). Faturamento por profissional é uma
        // aproximação — o produto não amarra lançamento ↔ profissional formalmente, então
        // usamos quem criou o lançamento como heurística.
        // TODO database-architect: campo lancamentos.profissional_usuario_id facilitaria
        // medir faturamento de forma exata (item de roadmap financeiro).
        const string sql = """
            WITH ativos AS (
                SELECT v.profissional_usuario_id
                FROM vinculo_profissional_estabelecimento v
                WHERE v.estabelecimento_id = @EstabelecimentoId
                  AND v.status = 'Ativo'
            ),
            ag AS (
                SELECT
                    profissional_usuario_id                                AS uid,
                    COUNT(*)                                               AS atend,
                    COUNT(*) FILTER (WHERE status = 'Concluido')           AS concluidos
                FROM agendamentos
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND inicio_previsto::date BETWEEN @DataInicio::date AND @DataFim::date
                GROUP BY profissional_usuario_id
            ),
            fin AS (
                SELECT
                    criado_por_usuario_id                                  AS uid,
                    COALESCE(SUM(valor), 0)                                AS faturamento
                FROM lancamentos
                WHERE estabelecimento_id = @EstabelecimentoId
                  AND tipo = 'receita'
                  AND status = 'Pago'
                  AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
                GROUP BY criado_por_usuario_id
            )
            SELECT
                ativos.profissional_usuario_id                             AS ProfissionalUsuarioId,
                COALESCE(u.nome_completo, 'Profissional #' || ativos.profissional_usuario_id::text)
                                                                           AS Nome,
                COALESCE(ag.atend, 0)                                      AS Atendimentos,
                COALESCE(ag.concluidos, 0)                                 AS AtendimentosConcluidos,
                COALESCE(fin.faturamento, 0)                               AS Faturamento,
                CASE WHEN COALESCE(ag.atend, 0) > 0
                     THEN ROUND((COALESCE(ag.concluidos, 0)::numeric / ag.atend) * 100, 2)
                     ELSE 0 END                                            AS TaxaOcupacao
            FROM ativos
            LEFT JOIN ag  ON ag.uid  = ativos.profissional_usuario_id
            LEFT JOIN fin ON fin.uid = ativos.profissional_usuario_id
            LEFT JOIN usuarios u ON u.id = ativos.profissional_usuario_id
            ORDER BY AtendimentosConcluidos DESC, Faturamento DESC
            """;

        var lista = (await conn.QueryAsync<DesempenhoProfissionalDto>(sql, p)).ToList();

        return new ProfissionaisResumoDto { Desempenho = lista };
    }

    // -------------------------------------------------------------------------
    // 4) RELATÓRIO DE ORÇAMENTOS
    //    Substitui rpc_report_budgets_summary.
    // -------------------------------------------------------------------------

    public async Task<RelatorioOrcamentosDto> RelatorioOrcamentos(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        };

        // Total do orçamento = SUM(itens.subtotal) + custo_implantes_total +
        //                     SUM(orcamento_cirurgias.valor_total) +
        //                     COALESCE(orcamento_internacao.valor_total, 0)
        // — espelha o getter Orcamento.Total no domínio. Anestesia não tem campo de
        // valor total persistido isolado; fica a cargo de versão futura.
        // Batch único: 1 round-trip ao Postgres em vez de 2.
        // Ordem dos SELECTs: resumo (single) → breakdown por status.
        // Nota: CTE `base` é declarada por statement (CTEs não persistem entre statements
        // separados por ';' em Postgres) — comportamento idêntico ao código anterior.
        const string sqlBatch = """
            WITH base AS (
                SELECT
                    o.id,
                    o.status,
                    COALESCE(itens.total, 0)
                      + o.custo_implantes_total
                      + COALESCE(cir.total, 0)
                      + COALESCE(intn.valor_total, 0)                AS valor_total
                FROM orcamentos o
                LEFT JOIN (
                    SELECT orcamento_id, SUM(subtotal) AS total
                    FROM itens_orcamento GROUP BY orcamento_id
                ) itens ON itens.orcamento_id = o.id
                LEFT JOIN (
                    SELECT orcamento_id, SUM(valor_total) AS total
                    FROM orcamento_cirurgias GROUP BY orcamento_id
                ) cir   ON cir.orcamento_id = o.id
                LEFT JOIN orcamento_internacao intn ON intn.orcamento_id = o.id
                WHERE o.estabelecimento_id = @EstabelecimentoId
                  AND o.criado_em::date BETWEEN @DataInicio::date AND @DataFim::date
            )
            SELECT
                COUNT(*)                                                              AS TotalEmitidos,
                COUNT(*) FILTER (WHERE status = 'Aprovado')                            AS TotalAprovados,
                COUNT(*) FILTER (WHERE status = 'Recusado')                            AS TotalRecusados,
                COALESCE(AVG(valor_total) FILTER (WHERE valor_total > 0), 0)           AS ValorMedio,
                CASE
                    WHEN (COUNT(*) FILTER (WHERE status IN ('Aprovado', 'Recusado'))) > 0
                    THEN ROUND(
                        (COUNT(*) FILTER (WHERE status = 'Aprovado')::numeric
                            / COUNT(*) FILTER (WHERE status IN ('Aprovado', 'Recusado'))) * 100,
                        2)
                    ELSE 0
                END                                                                    AS TaxaConversao
            FROM base;

            WITH base AS (
                SELECT
                    o.status,
                    COALESCE(itens.total, 0)
                      + o.custo_implantes_total
                      + COALESCE(cir.total, 0)
                      + COALESCE(intn.valor_total, 0) AS valor_total
                FROM orcamentos o
                LEFT JOIN (
                    SELECT orcamento_id, SUM(subtotal) AS total
                    FROM itens_orcamento GROUP BY orcamento_id
                ) itens ON itens.orcamento_id = o.id
                LEFT JOIN (
                    SELECT orcamento_id, SUM(valor_total) AS total
                    FROM orcamento_cirurgias GROUP BY orcamento_id
                ) cir   ON cir.orcamento_id = o.id
                LEFT JOIN orcamento_internacao intn ON intn.orcamento_id = o.id
                WHERE o.estabelecimento_id = @EstabelecimentoId
                  AND o.criado_em::date BETWEEN @DataInicio::date AND @DataFim::date
            )
            SELECT
                status                          AS Chave,
                COALESCE(SUM(valor_total), 0)   AS Valor,
                COUNT(*)                        AS Count
            FROM base
            GROUP BY status
            ORDER BY Count DESC;
            """;

        await using var grid = await conn.QueryMultipleAsync(sqlBatch, p);
        var resumo = await grid.ReadFirstAsync<RelatorioOrcamentosDto>();
        resumo.Breakdown = (await grid.ReadAsync<RowSummary>()).ToList();
        return resumo;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static DateTime? ParaDateTime(DateOnly? d)
        => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null;
}
