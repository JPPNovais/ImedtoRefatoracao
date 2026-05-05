using Dapper;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public record AgendamentoParaDisponibilidade(DateTime InicioPrevisto, DateTime FimPrevisto, string PacienteNome);

public class AgendamentoQueryRepository
{
    private readonly string _connStr;

    public AgendamentoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<AgendamentoDto>> Listar(
        long estabelecimentoId,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        Guid? profissionalUsuarioId,
        long? pacienteId,
        string? status)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var sql = """
            SELECT
                a.id                    AS Id,
                a.estabelecimento_id    AS EstabelecimentoId,
                a.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                a.profissional_usuario_id AS ProfissionalUsuarioId,
                COALESCE(uprf.nome_completo, uprf.email) AS ProfissionalNome,
                COALESCE(ucri.nome_completo, ucri.email) AS CriadoPorNome,
                a.inicio_previsto       AS InicioPrevisto,
                a.fim_previsto          AS FimPrevisto,
                a.tipo_servico          AS TipoServico,
                a.observacoes           AS Observacoes,
                a.status                AS Status,
                a.motivo_cancelamento   AS MotivoCancelamento,
                a.criado_em             AS CriadoEm,
                a.atualizado_em         AS AtualizadoEm
            FROM agendamentos a
            JOIN pacientes    pac  ON pac.id = a.paciente_id
            JOIN usuarios     uprf ON uprf.id = a.profissional_usuario_id
            JOIN usuarios     ucri ON ucri.id = a.criado_por_usuario_id
            WHERE a.estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::timestamp           IS NULL OR a.inicio_previsto::date >= @DataInicio::date)
              AND (@DataFim::timestamp              IS NULL OR a.inicio_previsto::date <= @DataFim::date)
              AND (@ProfissionalUsuarioId::uuid     IS NULL OR a.profissional_usuario_id = @ProfissionalUsuarioId::uuid)
              AND (@PacienteId::bigint              IS NULL OR a.paciente_id = @PacienteId::bigint)
              AND (@Status::text                    IS NULL OR a.status = @Status::text)
            ORDER BY a.inicio_previsto
            """;

        return await conn.QueryAsync<AgendamentoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MinValue) : null,
            ProfissionalUsuarioId = profissionalUsuarioId,
            PacienteId = pacienteId,
            Status = status
        });
    }

    public async Task<IEnumerable<ContagemPorDiaDto>> ContarPorDia(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim,
        Guid? profissionalUsuarioId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT  a.inicio_previsto::date AS Data,
                    COUNT(*)::int           AS Total
            FROM    agendamentos a
            WHERE   a.estabelecimento_id = @EstabelecimentoId
              AND   a.inicio_previsto::date >= @DataInicio::date
              AND   a.inicio_previsto::date <= @DataFim::date
              AND   (@ProfissionalUsuarioId::uuid IS NULL OR a.profissional_usuario_id = @ProfissionalUsuarioId::uuid)
            GROUP BY a.inicio_previsto::date
            """;

        return await conn.QueryAsync<ContagemPorDiaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            ProfissionalUsuarioId = profissionalUsuarioId,
        });
    }

    /// <summary>
    /// Retorna agendamentos ativos (não cancelados) de um profissional num intervalo de datas,
    /// com apenas os campos necessários para calcular disponibilidade.
    /// </summary>
    public async Task<IEnumerable<AgendamentoParaDisponibilidade>> ListarParaDisponibilidade(
        long estabelecimentoId,
        Guid profissionalUsuarioId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT  a.inicio_previsto   AS InicioPrevisto,
                    a.fim_previsto      AS FimPrevisto,
                    pac.nome_completo   AS PacienteNome
            FROM    agendamentos a
            JOIN    pacientes pac ON pac.id = a.paciente_id
            WHERE   a.estabelecimento_id = @EstabelecimentoId
              AND   a.profissional_usuario_id = @ProfissionalUsuarioId
              AND   a.status <> 'Cancelado'
              AND   a.inicio_previsto::date >= @DataInicio::date
              AND   a.inicio_previsto::date <= @DataFim::date
            ORDER BY a.inicio_previsto
            """;

        return await conn.QueryAsync<AgendamentoParaDisponibilidade>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
        });
    }

    public async Task<AgendamentoDto?> ObterPorId(long id, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Filtro por estabelecimento no SQL — defense-in-depth IDOR/LGPD.
        const string sql = """
            SELECT
                a.id                    AS Id,
                a.estabelecimento_id    AS EstabelecimentoId,
                a.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                a.profissional_usuario_id AS ProfissionalUsuarioId,
                COALESCE(uprf.nome_completo, uprf.email) AS ProfissionalNome,
                COALESCE(ucri.nome_completo, ucri.email) AS CriadoPorNome,
                a.inicio_previsto       AS InicioPrevisto,
                a.fim_previsto          AS FimPrevisto,
                a.tipo_servico          AS TipoServico,
                a.observacoes           AS Observacoes,
                a.status                AS Status,
                a.motivo_cancelamento   AS MotivoCancelamento,
                a.criado_em             AS CriadoEm,
                a.atualizado_em         AS AtualizadoEm
            FROM agendamentos a
            JOIN pacientes    pac  ON pac.id = a.paciente_id
            JOIN usuarios     uprf ON uprf.id = a.profissional_usuario_id
            JOIN usuarios     ucri ON ucri.id = a.criado_por_usuario_id
            WHERE a.id = @Id
              AND a.estabelecimento_id = @EstabelecimentoId
            """;

        return await conn.QuerySingleOrDefaultAsync<AgendamentoDto>(sql, new
        {
            Id = id,
            EstabelecimentoId = estabelecimentoId
        });
    }
}
