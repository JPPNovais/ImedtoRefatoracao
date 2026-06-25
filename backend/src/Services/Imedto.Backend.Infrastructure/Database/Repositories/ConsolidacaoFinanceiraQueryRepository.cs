using Dapper;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório Dapper singleton para as queries de consolidação do F7:
/// KPIs do período, extrato paginado com joins, caixa diário on-the-fly,
/// comissões por profissional (regime caixa), custo/lucro por paciente.
///
/// Toda query filtra por estabelecimento_id — falha-fechada (R12/LGPD).
/// Cálculo monetário usa ROUND(..., 2) no SQL — consistente com ArredondamentoMonetario no backend.
/// </summary>
public class ConsolidacaoFinanceiraQueryRepository
{
    private readonly string _connStr;

    public ConsolidacaoFinanceiraQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    // ────────────────────────────────────────────────────────────────────────────
    // KPIs do período (R1/CA156/CA157)
    // ────────────────────────────────────────────────────────────────────────────

    public virtual async Task<KpisFinanceiroDto> ObterKpis(long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Batch 3 SELECTs (1 round-trip):
        //   1) lancamentos — Recebido/Despesas/Estornos (regime caixa: data_pagamento),
        //      AReceberLancamentos = lançamentos Receita Pendentes avulsos (sem filtro de data).
        //   2) cobrancas em aberto — saldo a receber não coberto por lançamentos (INV-3: cobrança
        //      paga vira Lancamento Pago; cobrança em aberto NÃO tem lançamento Receita Pendente,
        //      portanto somar os dois não duplica — R1/CA3/2026-06-24_002).
        //      Saldo = valor_cobrado − desconto − SUM(pagamentos líquidos); cobranças Aberta/ParcialmentePaga.
        //   3) KPIs secundários (descontos/taxas no período).
        //
        // Regime de datas (decisão produto 2026-06-24):
        //   Recebido / Despesas / Estornos → regime caixa: data_pagamento dentro do período selecionado.
        //   "A receber" → estoque corrente (sem filtro de data); prazo exibido por data_vencimento na UI.
        const string sql = """
            SELECT
                COALESCE(SUM(CASE WHEN tipo = 'Receita' AND status = 'Pago'
                                       AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
                                  THEN valor ELSE 0 END), 0) AS Recebido,
                COALESCE(SUM(CASE WHEN tipo = 'Receita' AND status = 'Pendente'
                                  THEN valor ELSE 0 END), 0) AS AReceberLancamentos,
                COALESCE(SUM(CASE WHEN tipo = 'Despesa' AND status = 'Pago'
                                       AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
                                  THEN valor ELSE 0 END), 0) AS Despesas,
                COALESCE(SUM(CASE WHEN tipo = 'Receita' AND status = 'Pago'
                                       AND categoria = 'Estorno: Pagamento'
                                       AND data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
                                  THEN ABS(valor) ELSE 0 END), 0) AS Estornos
            FROM lancamentos
            WHERE estabelecimento_id = @EstabelecimentoId;

            SELECT
                COALESCE(SUM(
                    c.valor_cobrado
                    - COALESCE(c.desconto, 0)
                    - COALESCE(pg_agg.total_pago_liquido, 0)
                ), 0) AS SaldoCobrancasAberto
            FROM cobrancas c
            LEFT JOIN (
                SELECT cobranca_id, SUM(valor) AS total_pago_liquido
                FROM pagamentos
                WHERE cobranca_id IN (
                    SELECT id FROM cobrancas
                    WHERE estabelecimento_id = @EstabelecimentoId
                      AND status IN ('Aberta', 'ParcialmentePaga')
                )
                GROUP BY cobranca_id
            ) pg_agg ON pg_agg.cobranca_id = c.id
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND c.status IN ('Aberta', 'ParcialmentePaga');

            SELECT
                COALESCE(SUM(c.desconto), 0) AS DescontosConcedidos,
                COALESCE(SUM(p.taxa),     0) AS TaxasCartao
            FROM pagamentos p
            JOIN cobrancas c ON c.id = p.cobranca_id
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND p.data_pagamento BETWEEN @DataInicio::date AND @DataFim::date;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var kpis = await multi.ReadSingleAsync<KpisFinanceiroPrimarios>();
        var cobAberto = await multi.ReadSingleAsync<KpisCobrancasAberto>();
        var sec = await multi.ReadSingleAsync<KpisFinanceiroSecundarios>();

        // A receber = lançamentos Receita Pendentes avulsos + saldo de cobranças em aberto.
        // Não há dupla contagem: cobrança em aberto não tem lançamento Receita Pendente (INV-3).
        var aReceber = kpis.AReceberLancamentos + cobAberto.SaldoCobrancasAberto;

        return new KpisFinanceiroDto
        {
            Recebido = kpis.Recebido,
            AReceber = aReceber,
            Despesas = kpis.Despesas,
            Saldo = kpis.Recebido - kpis.Despesas,
            Estornos = kpis.Estornos,
            DescontosConcedidos = sec.DescontosConcedidos,
            TaxasCartao = sec.TaxasCartao
        };
    }

    private record KpisFinanceiroPrimarios(
        decimal Recebido, decimal AReceberLancamentos, decimal Despesas, decimal Estornos);

    private record KpisCobrancasAberto(decimal SaldoCobrancasAberto);

    private record KpisFinanceiroSecundarios(
        decimal DescontosConcedidos, decimal TaxasCartao);

    // ────────────────────────────────────────────────────────────────────────────
    // Extrato paginado com filtros (R2/R3/R4/CA158/CA159/CA161)
    // ────────────────────────────────────────────────────────────────────────────

    public virtual async Task<PaginaLancamentosExtratoDto> ListarExtrato(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim,
        string? tipo,
        string? categoria,
        string? formaPagamento,
        string? origem,
        int pagina,
        int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        await using var conn = new NpgsqlConnection(_connStr);

        // Join com cobranca → paciente quando CobrancaId não nulo (R3/LGPD).
        // Forma de pagamento vem do Pagamento quando existe; origem vem da Cobranca.
        // Filtros aplicados no WHERE (R4 — nunca client-side).
        // data_pagamento usa COALESCE com data_vencimento para incluir lançamentos pendentes
        // (que ainda não têm data_pagamento) — consistente com o ORDER BY abaixo.
        const string sqlBase = """
            FROM lancamentos l
            LEFT JOIN cobrancas c ON c.id = l.cobranca_id
            LEFT JOIN pacientes pac ON pac.id = c.paciente_id
            LEFT JOIN pagamentos pg ON pg.id = l.pagamento_id
            LEFT JOIN formas_pagamento fp ON fp.id = pg.forma_pagamento_id
            JOIN usuarios u ON u.id = l.criado_por_usuario_id
            WHERE l.estabelecimento_id = @EstabelecimentoId
              AND COALESCE(l.data_pagamento, l.data_vencimento) BETWEEN @DataInicio::date AND @DataFim::date
              AND (@Tipo::text          IS NULL OR l.tipo      = @Tipo)
              AND (@Categoria::text     IS NULL OR l.categoria = @Categoria)
              AND (@FormaPagamento::text IS NULL OR fp.nome    = @FormaPagamento)
              AND (@Origem::text         IS NULL OR c.tipo_atendimento::text = @Origem)
            """;

        var sql = $"""
            SELECT COUNT(*)::int {sqlBase};

            SELECT
                l.id                                                  AS Id,
                l.tipo                                                AS Tipo,
                l.descricao                                           AS Descricao,
                l.valor                                               AS Valor,
                l.data_pagamento                                      AS DataPagamento,
                l.data_vencimento                                     AS DataVencimento,
                l.status                                              AS Status,
                l.categoria                                           AS Categoria,
                fp.nome                                               AS FormaPagamento,
                c.tipo_atendimento::text                              AS Origem,
                l.cobranca_id                                         AS CobrancaId,
                CASE WHEN l.cobranca_id IS NOT NULL THEN pac.id       END AS PacienteId,
                CASE WHEN l.cobranca_id IS NOT NULL THEN pac.nome_completo END AS PacienteNome,
                COALESCE(u.nome_completo, u.email)                    AS CriadoPorNome
            {sqlBase}
            ORDER BY COALESCE(l.data_pagamento, l.data_vencimento) DESC, l.id DESC
            LIMIT @Tamanho OFFSET @Offset
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            Tipo = tipo,
            Categoria = categoria,
            FormaPagamento = formaPagamento,
            Origem = origem,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<LancamentoExtratoDto>();

        return new PaginaLancamentosExtratoDto
        {
            Itens = itens.ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Extrato modo vencidos — ignora período, filtra Pendente + vencimento < hoje
    // Mesma regra de DashboardQueryRepository (paridade CA13/R1/R4).
    // ────────────────────────────────────────────────────────────────────────────

    public virtual async Task<PaginaLancamentosExtratoDto> ListarExtratoVencidos(
        long estabelecimentoId,
        string? tipo,
        string? categoria,
        string? formaPagamento,
        string? origem,
        int pagina,
        int tamanhoPagina)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        await using var conn = new NpgsqlConnection(_connStr);

        // WHERE: status=Pendente, data_vencimento < hoje em America/Sao_Paulo (R3/CA10).
        // CURRENT_DATE usa UTC do servidor — substituído por horário de Brasília (briefing 2026-06-24_001).
        // Multi-tenant: WHERE l.estabelecimento_id = @EstabelecimentoId (falha-fechada).
        const string sqlBase = """
            FROM lancamentos l
            LEFT JOIN cobrancas c ON c.id = l.cobranca_id
            LEFT JOIN pacientes pac ON pac.id = c.paciente_id
            LEFT JOIN pagamentos pg ON pg.id = l.pagamento_id
            LEFT JOIN formas_pagamento fp ON fp.id = pg.forma_pagamento_id
            JOIN usuarios u ON u.id = l.criado_por_usuario_id
            WHERE l.estabelecimento_id = @EstabelecimentoId
              AND l.status = 'Pendente'
              AND l.data_vencimento < (now() AT TIME ZONE 'America/Sao_Paulo')::date
              AND (@Tipo::text           IS NULL OR l.tipo      = @Tipo)
              AND (@Categoria::text      IS NULL OR l.categoria = @Categoria)
              AND (@FormaPagamento::text IS NULL OR fp.nome     = @FormaPagamento)
              AND (@Origem::text         IS NULL OR c.tipo_atendimento::text = @Origem)
            """;

        var sql = $"""
            SELECT COUNT(*)::int {sqlBase};

            SELECT
                l.id                                                  AS Id,
                l.tipo                                                AS Tipo,
                l.descricao                                           AS Descricao,
                l.valor                                               AS Valor,
                l.data_pagamento                                      AS DataPagamento,
                l.data_vencimento                                     AS DataVencimento,
                l.status                                              AS Status,
                l.categoria                                           AS Categoria,
                fp.nome                                               AS FormaPagamento,
                c.tipo_atendimento::text                              AS Origem,
                l.cobranca_id                                         AS CobrancaId,
                CASE WHEN l.cobranca_id IS NOT NULL THEN pac.id       END AS PacienteId,
                CASE WHEN l.cobranca_id IS NOT NULL THEN pac.nome_completo END AS PacienteNome,
                COALESCE(u.nome_completo, u.email)                    AS CriadoPorNome
            {sqlBase}
            ORDER BY l.data_vencimento ASC, l.id ASC
            LIMIT @Tamanho OFFSET @Offset
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Categoria = categoria,
            FormaPagamento = formaPagamento,
            Origem = origem,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<LancamentoExtratoDto>();

        return new PaginaLancamentosExtratoDto
        {
            Itens = itens.ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Caixa diário — header + resumo on-the-fly (R8/CA163/CA164/CA170)
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<CaixaDiarioDto?> ObterCaixaDiario(long estabelecimentoId, DateOnly data)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Batch: header do caixa + resumo por forma de pagamento on-the-fly.
        // Resumo filtra por data_pagamento = data do caixa (sempre recalculado — R8).
        const string sql = """
            SELECT
                cd.id                                                              AS Id,
                cd.data                                                            AS Data,
                cd.status                                                          AS Status,
                cd.aberto_por_usuario_id                                           AS AbertoPorUsuarioId,
                COALESCE(ua.nome_completo, ua.email)                               AS AbertoPorNome,
                cd.aberto_em                                                       AS AbertoEm,
                cd.fechado_por_usuario_id                                          AS FechadoPorUsuarioId,
                COALESCE(uf.nome_completo, uf.email)                               AS FechadoPorNome,
                cd.fechado_em                                                      AS FechadoEm,
                cd.observacao                                                      AS Observacao,
                cd.reaberto_por_usuario_id                                         AS ReabertoPorUsuarioId,
                cd.reaberto_em                                                     AS ReabertoEm
            FROM caixa_diario cd
            JOIN usuarios ua ON ua.id = cd.aberto_por_usuario_id
            LEFT JOIN usuarios uf ON uf.id = cd.fechado_por_usuario_id
            WHERE cd.estabelecimento_id = @EstabelecimentoId
              AND cd.data = @Data::date;

            SELECT
                COALESCE(fp.nome, 'Sem forma de pagamento')                        AS FormaPagamento,
                COALESCE(SUM(l.valor), 0)                                          AS Total
            FROM lancamentos l
            LEFT JOIN pagamentos pg ON pg.id = l.pagamento_id
            LEFT JOIN formas_pagamento fp ON fp.id = pg.forma_pagamento_id
            WHERE l.estabelecimento_id = @EstabelecimentoId
              AND l.status = 'Pago'
              AND l.data_pagamento = @Data::date
            GROUP BY fp.nome
            ORDER BY SUM(l.valor) DESC;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            Data = data.ToDateTime(TimeOnly.MinValue)
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var caixa = await multi.ReadFirstOrDefaultAsync<CaixaDiarioDto>();
        if (caixa is null) return null;

        var resumo = (await multi.ReadAsync<ResumoCaixaFormaPagamentoDto>()).ToList();

        // Estornos: valor negativo dos lançamentos do dia — separar para exibição (CA164).
        var totalEstornos = resumo
            .Where(r => r.Total < 0)
            .Sum(r => r.Total);

        caixa.ResumoPorForma = resumo.Where(r => r.Total > 0).ToList();
        caixa.TotalEstornos = totalEstornos;
        caixa.TotalDia = resumo.Sum(r => r.Total);

        return caixa;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Comissões por período — regime caixa (R17/R18/CA171/CA173/CA174)
    // ────────────────────────────────────────────────────────────────────────────

    public virtual async Task<ComissaoPeriodoDto> ObterComissoes(
        long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            PercentualPadrao = ComissaoConfig.PercentualPadrao
        };

        // Consulta/Procedimento: pagamentos recebidos no período × percentual config (ou padrão 30%).
        // Cirurgia: OrcamentoEquipe (valor fixo) rateado pelo proporcional recebido/cobrado.
        // Regime caixa: só pagamentos liquidados no período entram (R18/CA174).
        //
        // R1 (comissão líquida de estorno — briefing 2026-06-24_001):
        // Dois casos distintos:
        //   A) Estorno no mesmo período do pagamento: CTE estornos_mesmo_periodo abate via GREATEST(0,...).
        //      Pagamento fica com valor_liquido=0 e é excluído pelo WHERE pp.valor_liquido > 0 (CA2/CA15).
        //   B) Estorno cross-período (pagamento em mês anterior, estorno no período consultado):
        //      O pagamento não entra em pagamentos_periodo (data_pagamento fora do período).
        //      CTE abatimentos_cross_periodo captura esses estornos e gera linhas de base negativa.
        //      Filtro: ep.data_estorno no período E pg.data_pagamento fora do período (evita dupla contagem).
        // A UNION dos dois CTEs produz o conjunto correto sem duplicação (CA3/CA6).
        const string sqlConsultaProcedimento = """
            WITH estornos_mesmo_periodo AS (
                -- Estornos cujo data_estorno E pagamento original caem no mesmo período consultado.
                -- Usados para abater o pagamento via GREATEST(0,...) no CTE pagamentos_periodo.
                SELECT
                    ep.pagamento_id,
                    ep.valor                                                            AS valor_estornado
                FROM estorno_pagamentos ep
                JOIN pagamentos pg ON pg.id = ep.pagamento_id
                WHERE ep.estabelecimento_id = @EstabelecimentoId
                  AND ep.data_estorno BETWEEN @DataInicio::date AND @DataFim::date
                  AND pg.data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            ),
            pagamentos_periodo AS (
                -- Pagamentos recebidos no período, com abatimento de estornos do mesmo período.
                SELECT
                    pg.id                                                               AS pagamento_id,
                    pg.cobranca_id,
                    pg.data_pagamento,
                    GREATEST(0,
                        pg.valor - COALESCE(pg.taxa, 0) - COALESCE(est.valor_estornado, 0)
                    )                                                                   AS valor_liquido
                FROM pagamentos pg
                JOIN cobrancas c ON c.id = pg.cobranca_id
                LEFT JOIN estornos_mesmo_periodo est ON est.pagamento_id = pg.id
                WHERE c.estabelecimento_id = @EstabelecimentoId
                  AND pg.data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            ),
            abatimentos_cross_periodo AS (
                -- Estornos cujo data_estorno cai no período consultado MAS cujo pagamento original
                -- NÃO cai no período (cross-período: pagamento em mês anterior, estorno agora).
                -- Gera base negativa para abater a comissão no período do estorno (R1 — decisão Q1).
                -- Filtro de tenant obrigatório — falha-fechada (R5).
                -- ep.valor = pg.valor por invariante do domínio (UNIQUE pagamento_id em estorno_pagamentos).
                -- Base negativa = -(pg.valor - pg.taxa) espelha o valor_liquido que o pagamento teria gerado.
                SELECT
                    ep.pagamento_id,
                    pg.cobranca_id,
                    ep.data_estorno                                                     AS data_pagamento,
                    -(pg.valor - COALESCE(pg.taxa, 0))                                  AS valor_liquido
                FROM estorno_pagamentos ep
                JOIN pagamentos pg ON pg.id = ep.pagamento_id
                WHERE ep.estabelecimento_id = @EstabelecimentoId
                  AND ep.data_estorno BETWEEN @DataInicio::date AND @DataFim::date
                  AND pg.data_pagamento NOT BETWEEN @DataInicio::date AND @DataFim::date
            ),
            cobrancas_cp AS (
                SELECT
                    c.id                                                                AS cobranca_id,
                    c.paciente_id,
                    c.origem,
                    COALESCE(a.profissional_usuario_id, c.criado_por_usuario_id)       AS profissional_id
                FROM cobrancas c
                LEFT JOIN agendamentos a ON a.id = c.agendamento_id
                WHERE c.estabelecimento_id = @EstabelecimentoId
                  AND c.origem IN ('Consulta', 'Procedimento')
            ),
            todas_linhas AS (
                -- União: pagamentos do período (exceto zerados) + abatimentos cross-período.
                SELECT pagamento_id, cobranca_id, data_pagamento, valor_liquido
                FROM pagamentos_periodo
                WHERE valor_liquido > 0
                UNION ALL
                SELECT pagamento_id, cobranca_id, data_pagamento, valor_liquido
                FROM abatimentos_cross_periodo
            )
            SELECT
                cc.profissional_id                                                      AS ProfissionalUsuarioId,
                COALESCE(u.nome_completo, u.email, cc.profissional_id::text)            AS Nome,
                v.especialidade_convidada                                               AS Especialidade,
                COUNT(*)::int                                                            AS Atendimentos,
                SUM(tl.valor_liquido)                                                   AS Faturamento,
                COALESCE(cfg.percentual, @PercentualPadrao)                             AS PercentualConfig,
                ROUND(SUM(tl.valor_liquido) * COALESCE(cfg.percentual, @PercentualPadrao) / 100, 2) AS Comissao,
                pac.id                                                                  AS PacienteId,
                pac.nome_completo                                                       AS PacienteNome,
                cc.origem                                                               AS TipoAtendimento,
                tl.data_pagamento                                                       AS Data,
                tl.valor_liquido                                                        AS BaseAtendimento,
                ROUND(tl.valor_liquido * COALESCE(cfg.percentual, @PercentualPadrao) / 100, 2) AS ComissaoAtendimento
            FROM todas_linhas tl
            JOIN cobrancas_cp cc ON cc.cobranca_id = tl.cobranca_id
            JOIN usuarios u ON u.id = cc.profissional_id
            LEFT JOIN vinculo_profissional_estabelecimento v
                   ON v.profissional_usuario_id = cc.profissional_id
                  AND v.estabelecimento_id = @EstabelecimentoId
                  AND v.status = 'Ativo'
            LEFT JOIN config_comissao_profissional cfg
                   ON cfg.estabelecimento_id = @EstabelecimentoId
                  AND cfg.profissional_usuario_id = cc.profissional_id
                  AND cfg.tipo = cc.origem
            LEFT JOIN pacientes pac ON pac.id = cc.paciente_id
            GROUP BY cc.profissional_id, u.nome_completo, u.email, v.especialidade_convidada,
                     cfg.percentual, pac.id, pac.nome_completo,
                     cc.origem, tl.data_pagamento, tl.valor_liquido
            ORDER BY cc.profissional_id, tl.data_pagamento
            """;

        // Cirurgia: OrcamentoEquipe define valor absoluto por profissional.
        // Rateio proporcional ao recebido_liquido/cobrado (R18/R2 — regime caixa, líquido de estorno).
        //
        // R2 cross-período: mesmo princípio da query CP.
        //   - pag_cirurgia: pagamentos no período, abatidos de estornos do mesmo período.
        //   - abatimentos_cross_cirurgia: estornos cujo data_estorno cai no período
        //     mas pagamento original não (cross-período) → geram linhas de total_pago negativo.
        // UNION garante que cobranças com apenas estorno cross (sem pag no período) também entram.
        const string sqlCirurgia = """
            WITH estornos_mesmo_periodo AS (
                SELECT
                    ep.pagamento_id,
                    ep.valor                                                    AS valor_estornado
                FROM estorno_pagamentos ep
                JOIN pagamentos pg ON pg.id = ep.pagamento_id
                WHERE ep.estabelecimento_id = @EstabelecimentoId
                  AND ep.data_estorno BETWEEN @DataInicio::date AND @DataFim::date
                  AND pg.data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
            ),
            pag_cirurgia AS (
                SELECT
                    c.id                                                        AS cobranca_id,
                    c.orcamento_id,
                    c.paciente_id,
                    SUM(GREATEST(0, pg.valor - COALESCE(est.valor_estornado, 0)))
                                                                                AS total_pago,
                    c.valor_cobrado                                             AS total_cobrado,
                    MAX(pg.data_pagamento)                                      AS data_ref
                FROM cobrancas c
                JOIN pagamentos pg ON pg.cobranca_id = c.id
                LEFT JOIN estornos_mesmo_periodo est ON est.pagamento_id = pg.id
                WHERE c.estabelecimento_id = @EstabelecimentoId
                  AND c.origem = 'Cirurgia'
                  AND pg.data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
                GROUP BY c.id, c.orcamento_id, c.paciente_id, c.valor_cobrado
            ),
            abatimentos_cross_cirurgia AS (
                -- Estornos cross-período: pagamento original fora do período consultado.
                -- total_pago negativo → reduz o rateio no período do estorno (R2/R1 cross-período).
                SELECT
                    c.id                                                        AS cobranca_id,
                    c.orcamento_id,
                    c.paciente_id,
                    -SUM(ep.valor)                                              AS total_pago,
                    c.valor_cobrado                                             AS total_cobrado,
                    MAX(ep.data_estorno)                                        AS data_ref
                FROM estorno_pagamentos ep
                JOIN pagamentos pg ON pg.id = ep.pagamento_id
                JOIN cobrancas c ON c.id = pg.cobranca_id
                WHERE ep.estabelecimento_id = @EstabelecimentoId
                  AND ep.data_estorno BETWEEN @DataInicio::date AND @DataFim::date
                  AND pg.data_pagamento NOT BETWEEN @DataInicio::date AND @DataFim::date
                  AND c.origem = 'Cirurgia'
                GROUP BY c.id, c.orcamento_id, c.paciente_id, c.valor_cobrado
            ),
            todas_cirurgias AS (
                SELECT cobranca_id, orcamento_id, paciente_id, total_pago, total_cobrado, data_ref
                FROM pag_cirurgia
                UNION ALL
                SELECT cobranca_id, orcamento_id, paciente_id, total_pago, total_cobrado, data_ref
                FROM abatimentos_cross_cirurgia
            )
            SELECT
                oe.profissional_usuario_id                                      AS ProfissionalUsuarioId,
                COALESCE(u.nome_completo, u.email, oe.profissional_usuario_id::text) AS Nome,
                v.especialidade_convidada                                        AS Especialidade,
                COUNT(*)::int                                                     AS Atendimentos,
                ROUND(SUM(oe.valor * CASE WHEN tc.total_cobrado > 0
                    THEN tc.total_pago / tc.total_cobrado ELSE 0 END), 2)       AS Faturamento,
                NULL::numeric                                                    AS PercentualConfig,
                ROUND(SUM(oe.valor * CASE WHEN tc.total_cobrado > 0
                    THEN tc.total_pago / tc.total_cobrado ELSE 0 END), 2)       AS Comissao,
                pac.id                                                           AS PacienteId,
                pac.nome_completo                                                AS PacienteNome,
                'Cirurgia'                                                       AS TipoAtendimento,
                MAX(tc.data_ref)                                                 AS Data,
                oe.valor                                                         AS BaseAtendimento,
                ROUND(oe.valor * CASE WHEN tc.total_cobrado > 0
                    THEN tc.total_pago / tc.total_cobrado ELSE 0 END, 2)        AS ComissaoAtendimento
            FROM todas_cirurgias tc
            JOIN orcamento_equipe oe ON oe.orcamento_id = tc.orcamento_id
            JOIN usuarios u ON u.id = oe.profissional_usuario_id
            LEFT JOIN vinculo_profissional_estabelecimento v
                   ON v.profissional_usuario_id = oe.profissional_usuario_id
                  AND v.estabelecimento_id = @EstabelecimentoId
                  AND v.status = 'Ativo'
            LEFT JOIN pacientes pac ON pac.id = tc.paciente_id
            GROUP BY oe.profissional_usuario_id, u.nome_completo, u.email,
                     v.especialidade_convidada, pac.id, pac.nome_completo, oe.valor,
                     tc.total_cobrado, tc.total_pago
            """;

        var linhasCP = (await conn.QueryAsync<ComissaoLinhaRaw>(sqlConsultaProcedimento, p)).ToList();
        var linhasCir = (await conn.QueryAsync<ComissaoLinhaRaw>(sqlCirurgia, p)).ToList();

        var todasLinhas = linhasCP.Concat(linhasCir).ToList();

        // Agrupar por profissional
        var profissionais = todasLinhas
            .GroupBy(l => l.ProfissionalUsuarioId)
            .Select(g =>
            {
                var detalhes = g.Select(l => new ComissaoAtendimentoDto
                {
                    Data = DateOnly.FromDateTime(l.Data),
                    TipoAtendimento = l.TipoAtendimento,
                    PacienteId = l.PacienteId,
                    PacienteNome = l.PacienteNome,
                    Base = l.BaseAtendimento,
                    Faturamento = l.BaseAtendimento,
                    Comissao = l.ComissaoAtendimento,
                    TipoBase = l.PercentualConfig.HasValue ? "percentual" : "orcamento_equipe"
                }).ToList();

                // Atendimentos = cada linha com BaseAtendimento > 0 (R4/CA12 — briefing 2026-06-24_002).
                // Linhas de abatimento cross-período têm BaseAtendimento < 0 e não representam
                // um novo atendimento — excluídas da contagem para alinhar com a lista detalhada.
                var atendimentos = g.Count(l => l.BaseAtendimento > 0);

                return new ComissaoProfissionalDto
                {
                    ProfissionalUsuarioId = g.Key,
                    Nome = g.First().Nome,
                    Especialidade = g.First().Especialidade,
                    Atendimentos = atendimentos,
                    Faturamento = g.Sum(l => l.BaseAtendimento),
                    PercentualConfig = g.First().PercentualConfig,
                    Comissao = g.Sum(l => l.ComissaoAtendimento),
                    Atendimentos_Detalhes = detalhes
                };
            })
            .OrderByDescending(p => p.Comissao)
            .ToList();

        return new ComissaoPeriodoDto
        {
            TotalARepassar = profissionais.Sum(p => p.Comissao),
            Profissionais = profissionais
        };
    }

    private record ComissaoLinhaRaw(
        Guid ProfissionalUsuarioId,
        string Nome,
        string? Especialidade,
        int Atendimentos,
        decimal Faturamento,
        decimal? PercentualConfig,
        decimal Comissao,
        long? PacienteId,
        string? PacienteNome,
        string TipoAtendimento,
        DateTime Data,
        decimal BaseAtendimento,
        decimal ComissaoAtendimento);

    // ────────────────────────────────────────────────────────────────────────────
    // Export de extrato sem paginação (R7/R8/CA5/CA6/CA10)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna todas as linhas do extrato do período + filtros (sem paginação).
    /// Reutiliza o predicado WHERE de ListarExtrato — não duplica lógica de filtro.
    /// Multi-tenant: WHERE l.estabelecimento_id = @EstabelecimentoId (R8, falha-fechada).
    /// </summary>
    public virtual async Task<List<LancamentoExtratoDto>> ExportarExtrato(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim,
        string? tipo,
        string? categoria,
        string? formaPagamento,
        string? origem)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Mesmo predicado FROM+WHERE da ListarExtrato — sem LIMIT/OFFSET.
        // COALESCE data_pagamento / data_vencimento para incluir lançamentos pendentes.
        const string sqlBase = """
            FROM lancamentos l
            LEFT JOIN cobrancas c ON c.id = l.cobranca_id
            LEFT JOIN pacientes pac ON pac.id = c.paciente_id
            LEFT JOIN pagamentos pg ON pg.id = l.pagamento_id
            LEFT JOIN formas_pagamento fp ON fp.id = pg.forma_pagamento_id
            JOIN usuarios u ON u.id = l.criado_por_usuario_id
            WHERE l.estabelecimento_id = @EstabelecimentoId
              AND COALESCE(l.data_pagamento, l.data_vencimento) BETWEEN @DataInicio::date AND @DataFim::date
              AND (@Tipo::text          IS NULL OR l.tipo      = @Tipo)
              AND (@Categoria::text     IS NULL OR l.categoria = @Categoria)
              AND (@FormaPagamento::text IS NULL OR fp.nome    = @FormaPagamento)
              AND (@Origem::text         IS NULL OR c.tipo_atendimento::text = @Origem)
            """;

        var sql = $"""
            SELECT
                l.id                                                  AS Id,
                l.tipo                                                AS Tipo,
                l.descricao                                           AS Descricao,
                l.valor                                               AS Valor,
                l.data_pagamento                                      AS DataPagamento,
                l.data_vencimento                                     AS DataVencimento,
                l.status                                              AS Status,
                l.categoria                                           AS Categoria,
                fp.nome                                               AS FormaPagamento,
                c.tipo_atendimento::text                              AS Origem,
                l.cobranca_id                                         AS CobrancaId,
                CASE WHEN l.cobranca_id IS NOT NULL THEN pac.id       END AS PacienteId,
                CASE WHEN l.cobranca_id IS NOT NULL THEN pac.nome_completo END AS PacienteNome,
                COALESCE(u.nome_completo, u.email)                    AS CriadoPorNome
            {sqlBase}
            ORDER BY COALESCE(l.data_pagamento, l.data_vencimento) DESC, l.id DESC
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            Tipo = tipo,
            Categoria = categoria,
            FormaPagamento = formaPagamento,
            Origem = origem
        };

        return (await conn.QueryAsync<LancamentoExtratoDto>(sql, p)).ToList();
    }

    /// <summary>
    /// Audit LGPD best-effort do export (CA10/R9).
    /// Sem PII: apenas contagem, período e identidade do usuário.
    /// Falha é engolida — não bloqueia o export.
    /// </summary>
    public virtual async Task GravarExportAuditAsync(
        Guid usuarioId,
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim,
        int totalLinhas)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.ExecuteAsync(
                """
                INSERT INTO financeiro_export_log
                    (usuario_id, estabelecimento_id, acao, periodo_inicio, periodo_fim, total_linhas, ocorrido_em)
                VALUES
                    (@UsuarioId, @EstabelecimentoId, 'ExportarExtrato', @DataInicio, @DataFim, @TotalLinhas, now())
                """,
                new
                {
                    UsuarioId = usuarioId,
                    EstabelecimentoId = estabelecimentoId,
                    DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
                    DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
                    TotalLinhas = totalLinhas
                });
        }
        catch
        {
            // Best-effort: falha de audit não bloqueia o export.
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Config de comissão por profissional (R16)
    // ────────────────────────────────────────────────────────────────────────────

    public async Task<(decimal? consulta, decimal? procedimento)> ObterConfigComissao(
        long estabelecimentoId, Guid profissionalUsuarioId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var rows = await conn.QueryAsync<(string Tipo, decimal Percentual)>(
            """
            SELECT tipo AS Tipo, percentual AS Percentual
            FROM config_comissao_profissional
            WHERE estabelecimento_id = @EstabelecimentoId
              AND profissional_usuario_id = @ProfissionalUsuarioId
            """,
            new { EstabelecimentoId = estabelecimentoId, ProfissionalUsuarioId = profissionalUsuarioId });

        decimal? consulta = null;
        decimal? procedimento = null;
        foreach (var row in rows)
        {
            if (row.Tipo == "Consulta") consulta = row.Percentual;
            if (row.Tipo == "Procedimento") procedimento = row.Percentual;
        }
        return (consulta, procedimento);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Custo/lucro por paciente para Relatórios (R20/R21/CA179/CA180)
    // ────────────────────────────────────────────────────────────────────────────

    public virtual async Task<List<CustoLucroPacienteDto>> ObterCustoLucroPorPaciente(
        long estabelecimentoId, DateOnly dataInicio, DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Regime caixa (decisão Q3/R3 — briefing 2026-06-24_002): o período filtra pagamentos por
        // data_pagamento, não por criado_em da cobrança. Um paciente cuja cobrança foi criada fora
        // do período mas paga dentro dele aparece no relatório; "Cobrado"/"Desconto" são da cobrança
        // que originou o pagamento (contexto da receita realizada).
        //
        // CTE pago: filtra pagamentos no período (data_pagamento) — agrega valor, taxa por cobrança.
        // CTE custo: custos de insumo atribuídos via cobranca_id (sem filtro de data — custo é da cirurgia).
        // JOIN em cobrancas: garante multi-tenant (estabelecimento_id) e obtém valor_cobrado/desconto.
        // LGPD: nome do paciente no DTO minimizado (R22 — só id+nome; drill-down audita no handler).
        const string sqlOtimizado = """
            WITH pago AS (
                -- Regime caixa: apenas pagamentos com data_pagamento no período.
                SELECT pg.cobranca_id,
                       SUM(COALESCE(pg.valor, 0)) AS total_pago,
                       SUM(COALESCE(pg.taxa, 0))  AS total_taxa
                FROM pagamentos pg
                JOIN cobrancas c ON c.id = pg.cobranca_id
                WHERE c.estabelecimento_id = @EstabelecimentoId
                  AND pg.data_pagamento BETWEEN @DataInicio::date AND @DataFim::date
                GROUP BY pg.cobranca_id
            ),
            custo AS (
                SELECT me.cobranca_id, SUM(me.custo_total) AS total_custo
                FROM movimentacoes_estoque me
                WHERE me.estabelecimento_id = @EstabelecimentoId
                  AND me.cobranca_id IS NOT NULL
                  AND me.deletado_em IS NULL
                GROUP BY me.cobranca_id
            )
            SELECT
                pac.id                                                      AS PacienteId,
                pac.nome_completo                                           AS PacienteNome,
                ROUND(SUM(c.valor_cobrado - COALESCE(c.desconto, 0)), 2)   AS Cobrado,
                ROUND(SUM(COALESCE(pg.total_pago, 0)), 2)                  AS Pago,
                ROUND(SUM(COALESCE(c.desconto, 0)), 2)                     AS Desconto,
                ROUND(SUM(COALESCE(pg.total_taxa, 0)), 2)                  AS Taxa,
                ROUND(SUM(COALESCE(cu.total_custo, 0)), 2)                 AS Custo
            FROM pago pg
            JOIN cobrancas c ON c.id = pg.cobranca_id
            JOIN pacientes pac ON pac.id = c.paciente_id
            LEFT JOIN custo cu ON cu.cobranca_id = c.id
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND pac.deletado_em IS NULL
            GROUP BY pac.id, pac.nome_completo
            ORDER BY pac.nome_completo
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue)
        };

        var rows = (await conn.QueryAsync<CustoLucroPacienteRaw>(sqlOtimizado, p)).ToList();

        return rows.Select(r => new CustoLucroPacienteDto
        {
            PacienteId = r.PacienteId,
            PacienteNome = r.PacienteNome,
            Cobrado = r.Cobrado,
            Pago = r.Pago,
            Desconto = r.Desconto,
            Taxa = r.Taxa,
            Custo = r.Custo,
            Lucro = r.Pago - r.Custo
        }).ToList();
    }

    private record CustoLucroPacienteRaw(
        long PacienteId, string PacienteNome,
        decimal Cobrado, decimal Pago, decimal Desconto, decimal Taxa, decimal Custo);
}
