using Dapper;
using Imedto.Backend.Contracts.Dashboard;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class DashboardQueryRepository
{
    private readonly string _connStr;

    public DashboardQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<DashboardDto> ObterDashboard(long estabelecimentoId)
    {
        // Batch único (3 SELECTs separados por ';') executado via QueryMultipleAsync —
        // 1 round-trip ao Postgres em vez de 3. Reduz latência total para latência da
        // pior query individual em vez de soma das três.
        //
        // Frente B (briefing 2026-06-24_001): datas "hoje/agora" em America/Sao_Paulo.
        // CURRENT_DATE usa UTC do servidor — substituído por (now() AT TIME ZONE 'America/Sao_Paulo')::date.
        // Impacto: AgendamentosHoje e LancamentosVencidos/VencidosAReceber/VencidosAPagar usam o dia
        // correto em Brasília, sem pular para o dia seguinte na virada das 21h BRT (CA8/CA10).
        //
        // Frente D (briefing 2026-06-24_002): ReceitasMes/DespesasMes usam regime caixa (data_pagamento).
        // Antes usavam data_vencimento — um "recebido do mês" diferia do KPI "Recebido" da Visão geral.
        // Agora ambos usam data_pagamento, e o mês é ancorando em Brasília (mesma timezone do 001).
        const string sqlBatch = """
            SELECT
                (SELECT COUNT(*) FROM pacientes
                    WHERE estabelecimento_id = @EstabId AND deletado_em IS NULL)                              AS TotalPacientesAtivos,
                (SELECT COUNT(*) FROM agendamentos
                    WHERE estabelecimento_id = @EstabId
                      AND inicio_previsto::date = (now() AT TIME ZONE 'America/Sao_Paulo')::date
                      AND status NOT IN ('Cancelado'))                                                        AS AgendamentosHoje,
                (SELECT COUNT(*) FROM agendamentos
                    WHERE estabelecimento_id = @EstabId
                      AND inicio_previsto >= NOW()
                      AND inicio_previsto <= NOW() + INTERVAL '7 days'
                      AND status NOT IN ('Cancelado'))                                                        AS AgendamentosSemana,
                (SELECT COALESCE(SUM(valor), 0) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND tipo = 'Receita' AND status = 'Pago'
                      AND data_pagamento IS NOT NULL
                      AND date_trunc('month', data_pagamento AT TIME ZONE 'America/Sao_Paulo')
                        = date_trunc('month', (now() AT TIME ZONE 'America/Sao_Paulo')))                      AS ReceitasMes,
                (SELECT COALESCE(SUM(valor), 0) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND tipo = 'Despesa' AND status = 'Pago'
                      AND data_pagamento IS NOT NULL
                      AND date_trunc('month', data_pagamento AT TIME ZONE 'America/Sao_Paulo')
                        = date_trunc('month', (now() AT TIME ZONE 'America/Sao_Paulo')))                      AS DespesasMes,
                (SELECT COUNT(*) FROM itens_inventario
                    WHERE estabelecimento_id = @EstabId AND ativo = true
                      AND quantidade_atual < quantidade_minima)                                               AS ItensAbaixoMinimo,
                (SELECT COUNT(*) FROM orcamentos
                    WHERE estabelecimento_id = @EstabId AND status IN ('Rascunho','Enviado'))               AS OrcamentosPendentes,
                (SELECT COUNT(*) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND status = 'Pendente'
                      AND data_vencimento < (now() AT TIME ZONE 'America/Sao_Paulo')::date)                  AS LancamentosVencidos,
                -- Valores de vencidos por tipo — mesma regra de LancamentosVencidos (paridade CA13).
                (SELECT COALESCE(SUM(valor), 0) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND tipo = 'Receita'
                      AND status = 'Pendente'
                      AND data_vencimento < (now() AT TIME ZONE 'America/Sao_Paulo')::date)                  AS VencidosAReceber,
                (SELECT COALESCE(SUM(valor), 0) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND tipo = 'Despesa'
                      AND status = 'Pendente'
                      AND data_vencimento < (now() AT TIME ZONE 'America/Sao_Paulo')::date)                  AS VencidosAPagar;

            SELECT
                a.id                    AS Id,
                pac.nome_completo       AS PacienteNome,
                COALESCE(u.nome_completo, u.email) AS ProfissionalNome,
                a.inicio_previsto       AS InicioPrevisto,
                a.tipo_servico          AS TipoServico,
                a.status                AS Status
            FROM agendamentos a
            JOIN pacientes pac ON pac.id = a.paciente_id
            JOIN usuarios   u   ON u.id  = a.profissional_usuario_id
            WHERE a.estabelecimento_id = @EstabId
              AND a.inicio_previsto >= NOW()
              AND a.status NOT IN ('Cancelado', 'Concluido')
            ORDER BY a.inicio_previsto
            LIMIT 5;

            SELECT id AS Id, nome AS Nome,
                   quantidade_atual AS QuantidadeAtual,
                   quantidade_minima AS QuantidadeMinima,
                   unidade_medida AS UnidadeMedida
            FROM itens_inventario
            WHERE estabelecimento_id = @EstabId AND ativo = true
              AND quantidade_atual < quantidade_minima
            ORDER BY (quantidade_minima - quantidade_atual) DESC
            LIMIT 5;
            """;

        var p = new { EstabId = estabelecimentoId };

        await using var conn = new NpgsqlConnection(_connStr);
        await using var grid = await conn.QueryMultipleAsync(sqlBatch, p);

        var kpis = await grid.ReadFirstAsync<DashboardDto>();
        kpis.ProximosAgendamentos = (await grid.ReadAsync<ProximoAgendamentoDto>()).ToList();
        kpis.ItensAbaixoMinimoLista = (await grid.ReadAsync<ItemAbaixoMinimoDto>()).ToList();
        kpis.SaldoMes = kpis.ReceitasMes - kpis.DespesasMes;

        return kpis;
    }
}
