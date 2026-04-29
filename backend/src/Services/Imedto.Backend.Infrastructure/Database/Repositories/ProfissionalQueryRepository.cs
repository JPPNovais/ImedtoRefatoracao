using Dapper;
using Imedto.Backend.Contracts.Profissionais.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProfissionalQueryRepository
{
    private readonly string _connectionString;

    public ProfissionalQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<ProfissionalDto> ObterPorUsuario(Guid usuarioId)
    {
        const string sql = """
            SELECT  usuario_id      AS UsuarioId,
                    conselho        AS Conselho,
                    uf              AS Uf,
                    numero_registro AS NumeroRegistro,
                    especialidade   AS Especialidade,
                    bio             AS Bio,
                    foto_url        AS FotoUrl,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.profissionais
            WHERE   usuario_id = @UsuarioId
              AND   deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<ProfissionalDto>(sql, new { UsuarioId = usuarioId });
    }
}
