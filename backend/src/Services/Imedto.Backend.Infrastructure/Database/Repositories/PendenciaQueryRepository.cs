using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório Dapper para leituras de pendências de atendimento (singleton — query leve).
/// Usa o índice (estabelecimento_id, paciente_id, status) para performance (CA74).
/// Falha-fechada: sempre filtra por estabelecimento_id.
/// LGPD/minimização: retorna apenas tipo da ação, vínculos por id e datas (R4/CA71).
/// </summary>
public class PendenciaQueryRepository
{
    private readonly string _connStr;

    public PendenciaQueryRepository(AppReadConnectionString conn)
        => _connStr = conn.Value;

    public async Task<IReadOnlyList<PendenciaAbertaDto>> ListarAbertas(
        long pacienteId,
        long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                id          AS Id,
                evolucao_id AS EvolucaoId,
                acao        AS Acao,
                status      AS Status,
                criado_em   AS CriadoEm
            FROM pendencias_atendimento
            WHERE estabelecimento_id = @EstabelecimentoId
              AND paciente_id        = @PacienteId
              AND status             = 'Pendente'
            ORDER BY criado_em DESC;
            """;

        var resultado = await conn.QueryAsync<PendenciaAbertaDto>(
            sql,
            new { EstabelecimentoId = estabelecimentoId, PacienteId = pacienteId });

        return resultado.ToList();
    }
}
