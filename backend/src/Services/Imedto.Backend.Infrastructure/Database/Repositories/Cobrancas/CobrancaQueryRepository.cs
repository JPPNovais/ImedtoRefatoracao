using Dapper;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cobrancas;

/// <summary>
/// Repositório Dapper para leituras de cobrança/pagamento (singleton — queries rápidas).
/// Todos os métodos filtram por estabelecimento_id (R14 multi-tenant falha-fechada).
/// </summary>
public class CobrancaQueryRepository
{
    private readonly string _connStr;

    public CobrancaQueryRepository(AppReadConnectionString conn)
        => _connStr = conn.Value;

    /// <summary>Retorna detalhes de cobrança + histórico de pagamentos para o PaymentModal.</summary>
    public async Task<CobrancaDetalheDto?> ObterDetalhesPorAgendamento(long agendamentoId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                c.id                AS Id,
                c.tipo_atendimento  AS TipoAtendimento,
                c.valor_cobrado     AS ValorCobrado,
                c.desconto          AS Desconto,
                c.status            AS Status
            FROM cobrancas c
            WHERE c.agendamento_id = @AgendamentoId
              AND c.estabelecimento_id = @EstabelecimentoId
            LIMIT 1;

            SELECT
                p.id                    AS Id,
                p.valor                 AS Valor,
                fp.nome                 AS FormaPagamentoNome,
                p.parcelas              AS Parcelas,
                p.taxa                  AS Taxa,
                p.data_pagamento        AS DataPagamento
            FROM pagamentos p
            JOIN cobrancas c ON c.id = p.cobranca_id
            JOIN formas_pagamento fp ON fp.id = p.forma_pagamento_id
            WHERE c.agendamento_id = @AgendamentoId
              AND c.estabelecimento_id = @EstabelecimentoId
            ORDER BY p.criado_em;
            """;

        await using var multi = await conn.QueryMultipleAsync(sql, new { AgendamentoId = agendamentoId, EstabelecimentoId = estabelecimentoId });
        var cobranca = await multi.ReadSingleOrDefaultAsync<CobrancaDetalheDto>();
        if (cobranca is null) return null;

        var pagamentos = (await multi.ReadAsync<PagamentoResumoDto>()).ToList();
        cobranca.Pagamentos = pagamentos;

        // Calcula campos derivados no lado da leitura
        cobranca.TotalLiquido = cobranca.ValorCobrado - cobranca.Desconto;
        cobranca.TotalPago = pagamentos.Sum(p => p.Valor);
        cobranca.SaldoDevedor = cobranca.TotalLiquido - cobranca.TotalPago;

        return cobranca;
    }

    /// <summary>
    /// Retorna valor sugerido (profissional específico > padrão do estabelecimento).
    /// Usado pelo query handler de check-in (R2/CA2).
    /// </summary>
    public async Task<decimal?> ObterValorSugerido(long estabelecimentoId, Guid profissionalUsuarioId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT valor_sugerido
            FROM tabela_preco_consulta
            WHERE estabelecimento_id = @EstabelecimentoId
              AND profissional_id = @ProfissionalId
              AND ativo = true
            LIMIT 1;
            """;

        var valorProfissional = await conn.QuerySingleOrDefaultAsync<decimal?>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalId = profissionalUsuarioId
        });
        if (valorProfissional.HasValue) return valorProfissional;

        // Fallback: padrão do estabelecimento
        const string sqlPadrao = """
            SELECT valor_sugerido
            FROM tabela_preco_consulta
            WHERE estabelecimento_id = @EstabelecimentoId
              AND profissional_id IS NULL
              AND ativo = true
            LIMIT 1;
            """;
        return await conn.QuerySingleOrDefaultAsync<decimal?>(sqlPadrao, new { EstabelecimentoId = estabelecimentoId });
    }

    /// <summary>Lista tabela de preços com nome do profissional (CA19 — com debounce no front).</summary>
    public async Task<IEnumerable<TabelaPrecoConsultaDto>> ListarTabelaPreco(long estabelecimentoId, string? buscaProfissional)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                t.id                                            AS Id,
                t.profissional_id                               AS ProfissionalId,
                COALESCE(u.nome_completo, u.email)              AS ProfissionalNome,
                t.valor_sugerido                                AS ValorSugerido,
                t.ativo                                         AS Ativo
            FROM tabela_preco_consulta t
            LEFT JOIN usuarios u ON u.id = t.profissional_id
            WHERE t.estabelecimento_id = @EstabelecimentoId
              AND (
                    @Busca::text IS NULL
                    OR t.profissional_id IS NULL
                    OR unaccent(COALESCE(u.nome_completo, u.email)) ILIKE unaccent('%' || @Busca || '%')
              )
            ORDER BY t.profissional_id IS NOT NULL, COALESCE(u.nome_completo, u.email)
            """;

        return await conn.QueryAsync<TabelaPrecoConsultaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = string.IsNullOrWhiteSpace(buscaProfissional) ? null : buscaProfissional.Trim()
        });
    }

    /// <summary>Lista configurações de taxa por forma de pagamento com nome da forma.</summary>
    public async Task<IEnumerable<ConfigTaxaFormaPagamentoDto>> ListarConfigTaxa(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                ct.id                   AS Id,
                ct.forma_pagamento_id   AS FormaPagamentoId,
                fp.nome                 AS FormaPagamentoNome,
                ct.taxa_percentual      AS TaxaPercentual,
                ct.ativo                AS Ativo
            FROM config_taxa_forma_pagamento ct
            JOIN formas_pagamento fp ON fp.id = ct.forma_pagamento_id
            WHERE ct.estabelecimento_id = @EstabelecimentoId
            ORDER BY fp.nome
            """;

        return await conn.QueryAsync<ConfigTaxaFormaPagamentoDto>(sql, new { EstabelecimentoId = estabelecimentoId });
    }

    /// <summary>
    /// Retorna KPIs + lista completa de cobranças/pagamentos/estornos do paciente na aba Financeiro (F2).
    /// Multi-tenant: filtra por estabelecimento_id + paciente_id.
    /// DTO mínimo LGPD: sem dado clínico.
    /// </summary>
    public async Task<FinanceiroAbaDto> ObterFinanceiroAba(long pacienteId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Query 1: cobranças do paciente
        const string sqlCobrancas = """
            SELECT
                c.id                AS Id,
                c.origem            AS Origem,
                c.tipo_atendimento  AS TipoAtendimento,
                c.valor_cobrado     AS ValorCobrado,
                c.desconto          AS Desconto,
                c.status            AS Status,
                c.descricao         AS Descricao
            FROM cobrancas c
            WHERE c.paciente_id = @PacienteId
              AND c.estabelecimento_id = @EstabelecimentoId
            ORDER BY c.criado_em DESC;
            """;

        // Query 2: pagamentos das cobranças do paciente
        const string sqlPagamentos = """
            SELECT
                p.id                    AS Id,
                p.cobranca_id           AS CobrancaId,
                p.valor                 AS Valor,
                COALESCE(fp.nome, '')   AS FormaPagamentoNome,
                p.parcelas              AS Parcelas,
                p.taxa                  AS Taxa,
                p.data_pagamento        AS DataPagamento
            FROM pagamentos p
            JOIN cobrancas c ON c.id = p.cobranca_id
            LEFT JOIN formas_pagamento fp ON fp.id = p.forma_pagamento_id
            WHERE c.paciente_id = @PacienteId
              AND c.estabelecimento_id = @EstabelecimentoId
            ORDER BY p.criado_em;
            """;

        // Query 3: estornos das cobranças do paciente
        const string sqlEstornos = """
            SELECT
                ep.id                                   AS Id,
                ep.pagamento_id                         AS PagamentoId,
                ep.cobranca_id                          AS CobrancaId,
                ep.valor                                AS Valor,
                ep.motivo                               AS Motivo,
                COALESCE(u.nome_completo, u.email, '')  AS EstornadoPorNome,
                ep.data_estorno                         AS DataEstorno
            FROM estorno_pagamentos ep
            JOIN cobrancas c ON c.id = ep.cobranca_id
            LEFT JOIN usuarios u ON u.id = ep.estornado_por_usuario_id
            WHERE c.paciente_id = @PacienteId
              AND c.estabelecimento_id = @EstabelecimentoId
            ORDER BY ep.criado_em;
            """;

        // Query 4: histórico de valor de cirurgia das cobranças do paciente (F5/CA106).
        const string sqlHistoricoValor = """
            SELECT
                hv.cobranca_id                              AS CobrancaId,
                hv.valor_anterior                           AS ValorAnterior,
                hv.valor_novo                               AS ValorNovo,
                COALESCE(u.nome_completo, u.email, '')      AS AlteradoPorNome,
                hv.alterado_em                              AS AlteradoEm
            FROM cobranca_historico_valor hv
            JOIN cobrancas c ON c.id = hv.cobranca_id
            LEFT JOIN usuarios u ON u.id = hv.alterado_por_usuario_id
            WHERE c.paciente_id = @PacienteId
              AND c.estabelecimento_id = @EstabelecimentoId
            ORDER BY hv.alterado_em;
            """;

        var param = new { PacienteId = pacienteId, EstabelecimentoId = estabelecimentoId };

        // Executa as 4 queries em paralelo.
        var cobrancasTask       = conn.QueryAsync<CobrancaAbaRaw>(sqlCobrancas, param);
        var pagamentosTask      = conn.QueryAsync<PagamentoAbaRaw>(sqlPagamentos, param);
        var estornosTask        = conn.QueryAsync<EstornoAbaRaw>(sqlEstornos, param);
        var historicoValorTask  = conn.QueryAsync<HistoricoValorAbaRaw>(sqlHistoricoValor, param);

        await Task.WhenAll(cobrancasTask, pagamentosTask, estornosTask, historicoValorTask);

        var cobrancas      = (await cobrancasTask).ToList();
        var pagamentos     = (await pagamentosTask).ToList();
        var estornos       = (await estornosTask).ToList();
        var historicoValor = (await historicoValorTask).ToList();

        // Índices por cobranca_id / pagamento_id
        var pagamentosPorCobranca    = pagamentos.GroupBy(p => p.CobrancaId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var estornosPorPagamento     = estornos.GroupBy(e => e.PagamentoId)
            .ToDictionary(g => g.Key, g => g.First());
        var historicoPorCobranca     = historicoValor.GroupBy(h => h.CobrancaId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var cobrancasDtos = cobrancas.Select(c =>
        {
            var pags = pagamentosPorCobranca.TryGetValue(c.Id, out var pl) ? pl : new();
            var pagDtos = pags.Select(p =>
            {
                var estornado = estornosPorPagamento.TryGetValue(p.Id, out var est);
                return new PagamentoAbaDto
                {
                    Id = p.Id,
                    Valor = p.Valor,
                    FormaPagamentoNome = p.FormaPagamentoNome,
                    Parcelas = p.Parcelas,
                    Taxa = p.Taxa,
                    DataPagamento = p.DataPagamento,
                    Estornado = estornado,
                    Estorno = estornado ? new EstornoAbaDto
                    {
                        Id = est!.Id,
                        Valor = est.Valor,
                        Motivo = est.Motivo,
                        EstornadoPorNome = est.EstornadoPorNome,
                        DataEstorno = est.DataEstorno,
                    } : null,
                };
            }).ToList();

            var totalPago      = pags.Sum(p => p.Valor);
            var totalEstornado = estornos.Where(e => e.CobrancaId == c.Id).Sum(e => e.Valor);
            var totalLiquido   = c.ValorCobrado - c.Desconto;
            var totalPagoLiq   = totalPago - totalEstornado;
            var saldo          = totalLiquido - totalPagoLiq;

            return new CobrancaAbaDto
            {
                Id = c.Id,
                Origem = c.Origem,
                TipoAtendimento = c.TipoAtendimento,
                ValorCobrado = c.ValorCobrado,
                Desconto = c.Desconto,
                TotalLiquido = totalLiquido,
                TotalPagoLiquido = totalPagoLiq,
                Saldo = saldo,
                Status = c.Status,
                Descricao = c.Descricao,
                Pagamentos = pagDtos,
                HistoricoValor = historicoPorCobranca.TryGetValue(c.Id, out var hvList)
                    ? hvList.Select(h => new HistoricoValorAbaDto
                    {
                        ValorAnterior   = h.ValorAnterior,
                        ValorNovo       = h.ValorNovo,
                        AlteradoPorNome = h.AlteradoPorNome,
                        AlteradoEm      = h.AlteradoEm,
                    }).ToList()
                    : Array.Empty<HistoricoValorAbaDto>(),
            };
        }).ToList();

        // KPIs agregados (DC7/R3)
        var kpiTotalCobrado    = cobrancasDtos.Sum(c => c.TotalLiquido);
        var kpiTotalPagoLiq    = cobrancasDtos.Sum(c => c.TotalPagoLiquido);
        var kpiSaldo           = cobrancasDtos.Sum(c => c.Saldo);

        return new FinanceiroAbaDto
        {
            TotalCobrado     = kpiTotalCobrado,
            TotalPagoLiquido = kpiTotalPagoLiq,
            Saldo            = kpiSaldo,
            Cobrancas        = cobrancasDtos,
        };
    }

    // ── Tipos internos de mapeamento Dapper ──────────────────────────────────
    private class CobrancaAbaRaw
    {
        public long Id { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string TipoAtendimento { get; set; } = string.Empty;
        public decimal ValorCobrado { get; set; }
        public decimal Desconto { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Descricao { get; set; }
    }

    private class PagamentoAbaRaw
    {
        public long Id { get; set; }
        public long CobrancaId { get; set; }
        public decimal Valor { get; set; }
        public string FormaPagamentoNome { get; set; } = string.Empty;
        public int Parcelas { get; set; }
        public decimal Taxa { get; set; }
        public DateOnly DataPagamento { get; set; }
    }

    private class EstornoAbaRaw
    {
        public long Id { get; set; }
        public long PagamentoId { get; set; }
        public long CobrancaId { get; set; }
        public decimal Valor { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string EstornadoPorNome { get; set; } = string.Empty;
        public DateOnly DataEstorno { get; set; }
    }

    private class HistoricoValorAbaRaw
    {
        public long CobrancaId { get; set; }
        public decimal ValorAnterior { get; set; }
        public decimal ValorNovo { get; set; }
        public string AlteradoPorNome { get; set; } = string.Empty;
        public DateTime AlteradoEm { get; set; }
    }

    /// <summary>
    /// Retorna dados de badge de pagamento para uma lista de agendamentos (CA3 — anti-N+1).
    /// Usada pelo AgendamentoQueryRepository para enricher o DTO da agenda.
    /// </summary>
    public async Task<IEnumerable<AgendamentoBadgeCobrancaDto>> ObterBadgesPorAgendamentos(
        long estabelecimentoId,
        IEnumerable<long> agendamentoIds)
    {
        var ids = agendamentoIds.ToList();
        if (!ids.Any()) return Array.Empty<AgendamentoBadgeCobrancaDto>();

        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                c.id                                        AS CobrancaId,
                c.agendamento_id                            AS AgendamentoId,
                c.status                                    AS Status,
                c.valor_cobrado                             AS ValorCobrado,
                c.tipo_atendimento                          AS TipoAtendimento,
                c.desconto                                  AS Desconto,
                COALESCE(SUM(p.valor), 0)                   AS TotalPago
            FROM cobrancas c
            LEFT JOIN pagamentos p ON p.cobranca_id = c.id
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND c.agendamento_id = ANY(@Ids)
            GROUP BY c.id, c.agendamento_id, c.status, c.valor_cobrado, c.tipo_atendimento, c.desconto
            """;

        return await conn.QueryAsync<AgendamentoBadgeCobrancaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Ids = ids.ToArray()
        });
    }
}

/// <summary>DTO interno para enrichment do badge na query da agenda.</summary>
public class AgendamentoBadgeCobrancaDto
{
    public long CobrancaId { get; set; }
    public long AgendamentoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TipoAtendimento { get; set; } = string.Empty;
    public decimal ValorCobrado { get; set; }
    public decimal Desconto { get; set; }
    public decimal TotalPago { get; set; }
    public decimal SaldoDevedor => ValorCobrado - Desconto - TotalPago;
}
