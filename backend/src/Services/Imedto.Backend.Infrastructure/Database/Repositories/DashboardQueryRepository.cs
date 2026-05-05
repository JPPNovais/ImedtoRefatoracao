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
        const string sqlBatch = """
            SELECT
                (SELECT COUNT(*) FROM pacientes
                    WHERE estabelecimento_id = @EstabId AND deletado_em IS NULL)                              AS TotalPacientesAtivos,
                (SELECT COUNT(*) FROM agendamentos
                    WHERE estabelecimento_id = @EstabId
                      AND inicio_previsto::date = CURRENT_DATE
                      AND status NOT IN ('Cancelado'))                                                        AS AgendamentosHoje,
                (SELECT COUNT(*) FROM agendamentos
                    WHERE estabelecimento_id = @EstabId
                      AND inicio_previsto >= NOW()
                      AND inicio_previsto <= NOW() + INTERVAL '7 days'
                      AND status NOT IN ('Cancelado'))                                                        AS AgendamentosSemana,
                (SELECT COALESCE(SUM(valor), 0) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND tipo = 'Receita' AND status = 'Pago'
                      AND date_trunc('month', data_vencimento) = date_trunc('month', CURRENT_DATE))           AS ReceitasMes,
                (SELECT COALESCE(SUM(valor), 0) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND tipo = 'Despesa' AND status = 'Pago'
                      AND date_trunc('month', data_vencimento) = date_trunc('month', CURRENT_DATE))           AS DespesasMes,
                (SELECT COUNT(*) FROM itens_inventario
                    WHERE estabelecimento_id = @EstabId AND ativo = true
                      AND quantidade_atual < quantidade_minima)                                               AS ItensAbaixoMinimo,
                (SELECT COUNT(*) FROM orcamentos
                    WHERE estabelecimento_id = @EstabId AND status IN ('Rascunho','Enviado'))               AS OrcamentosPendentes,
                (SELECT COUNT(*) FROM lancamentos
                    WHERE estabelecimento_id = @EstabId AND status = 'Pendente'
                      AND data_vencimento < CURRENT_DATE)                                                    AS LancamentosVencidos;

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
