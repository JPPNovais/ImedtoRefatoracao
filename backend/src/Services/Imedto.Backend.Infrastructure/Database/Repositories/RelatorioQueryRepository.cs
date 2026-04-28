using Dapper;
using Imedto.Backend.Contracts.Relatorios;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class RelatorioQueryRepository
{
    private readonly string _connStr;

    public RelatorioQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

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
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MinValue) : null
        });
    }

    public async Task<RelatorioAgendamentosDto> RelatorioAgendamentos(
        long estabelecimentoId,
        DateOnly? dataInicio,
        DateOnly? dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlStatus = """
            SELECT status AS Status, COUNT(*) AS Quantidade
            FROM agendamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::date IS NULL OR inicio_previsto::date >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR inicio_previsto::date <= @DataFim::date)
            GROUP BY status
            ORDER BY Quantidade DESC
            """;

        const string sqlDia = """
            SELECT inicio_previsto::date AS Data, COUNT(*) AS Quantidade
            FROM agendamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::date IS NULL OR inicio_previsto::date >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR inicio_previsto::date <= @DataFim::date)
            GROUP BY inicio_previsto::date
            ORDER BY Data
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MinValue) : null
        };

        var porStatus = (await conn.QueryAsync<AgendamentosPorStatusDto>(sqlStatus, p)).ToList();
        var porDia = (await conn.QueryAsync<AgendamentosPorDiaDto>(sqlDia, p)).ToList();

        return new RelatorioAgendamentosDto
        {
            Total = porStatus.Sum(s => s.Quantidade),
            PorStatus = porStatus,
            PorDia = porDia
        };
    }
}
