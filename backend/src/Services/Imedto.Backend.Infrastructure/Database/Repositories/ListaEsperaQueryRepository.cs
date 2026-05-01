using Dapper;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ListaEsperaQueryRepository
{
    private readonly string _connStr;
    public ListaEsperaQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<ListaEsperaItemDto>> Listar(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        // Filtra apenas entradas não atendidas. Calcula tempo decorrido em minutos.
        const string sql = """
            SELECT
                le.id                            AS Id,
                le.paciente_id                   AS PacienteId,
                pac.nome_completo                AS PacienteNome,
                pac.telefone                     AS PacienteTelefone,
                le.motivo                        AS Motivo,
                le.profissional_preferido_id     AS ProfissionalPreferidoId,
                COALESCE(u.nome_completo, u.email) AS ProfissionalPreferidoNome,
                le.prioridade                    AS Prioridade,
                le.preferencia_periodo           AS PreferenciaPeriodo,
                le.criado_em                     AS CriadoEm,
                EXTRACT(EPOCH FROM (NOW() - le.criado_em))::int / 60 AS MinutosDesdeQueEntrou
            FROM lista_espera_agendamento le
            JOIN pacientes pac ON pac.id = le.paciente_id
            LEFT JOIN usuarios u ON u.id = le.profissional_preferido_id
            WHERE le.estabelecimento_id = @EstabelecimentoId
              AND le.atendido_em IS NULL
            ORDER BY
                CASE le.prioridade
                    WHEN 'Urgente' THEN 0
                    WHEN 'Prioritario' THEN 1
                    ELSE 2
                END,
                le.criado_em ASC
            """;
        return await conn.QueryAsync<ListaEsperaItemDto>(sql, new { EstabelecimentoId = estabelecimentoId });
    }
}
