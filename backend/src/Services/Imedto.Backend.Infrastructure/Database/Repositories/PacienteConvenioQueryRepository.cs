using Dapper;
using Imedto.Backend.Contracts.PacienteConvenios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório Dapper para leitura de carteirinhas de paciente (singleton).
/// numero_carteirinha é PII — exposto apenas nos métodos que retornam dados para tela autenticada.
/// Todos os métodos filtram por estabelecimento_id (multi-tenant falha-fechada).
/// </summary>
public class PacienteConvenioQueryRepository
{
    private readonly string _connStr;

    public PacienteConvenioQueryRepository(AppReadConnectionString conn)
        => _connStr = conn.Value;

    /// <summary>Carteirinhas do paciente para a aba Convênios.</summary>
    public async Task<IReadOnlyList<PacienteConvenioDto>> ListarPorPaciente(long pacienteId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                pc.id               AS Id,
                pc.convenio_id      AS ConvenioId,
                c.nome              AS ConvenioNome,
                pc.plano_id         AS PlanoId,
                cp.nome             AS PlanoNome,
                pc.numero_carteirinha AS NumeroCarteirinha,
                pc.validade         AS Validade,
                pc.ativo            AS Ativo
            FROM paciente_convenios pc
            JOIN convenios c ON c.id = pc.convenio_id
            LEFT JOIN convenio_planos cp ON cp.id = pc.plano_id
            WHERE pc.paciente_id = @PacienteId
              AND pc.estabelecimento_id = @EstabelecimentoId
            ORDER BY pc.ativo DESC, c.nome
            """;

        var resultado = await conn.QueryAsync<PacienteConvenioDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
        return resultado.ToList();
    }

    /// <summary>Carteirinhas ativas para pré-seleção no check-in (R8). Expõe numero para exibição informativa.</summary>
    public async Task<IReadOnlyList<CarteirinhaCheckInDto>> ListarAtivasPorPacienteParaCheckIn(
        long pacienteId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                pc.convenio_id      AS ConvenioId,
                c.nome              AS ConvenioNome,
                pc.plano_id         AS PlanoId,
                cp.nome             AS PlanoNome,
                pc.numero_carteirinha AS NumeroCarteirinha,
                pc.validade         AS Validade
            FROM paciente_convenios pc
            JOIN convenios c ON c.id = pc.convenio_id
            LEFT JOIN convenio_planos cp ON cp.id = pc.plano_id
            WHERE pc.paciente_id = @PacienteId
              AND pc.estabelecimento_id = @EstabelecimentoId
              AND pc.ativo = TRUE
              AND c.ativo = TRUE
            ORDER BY pc.validade DESC NULLS LAST
            """;

        var resultado = await conn.QueryAsync<CarteirinhaCheckInDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
        return resultado.ToList();
    }
}
