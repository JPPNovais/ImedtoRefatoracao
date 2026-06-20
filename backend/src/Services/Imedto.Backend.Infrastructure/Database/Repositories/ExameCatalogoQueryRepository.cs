using Dapper;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ExameCatalogoQueryRepository
{
    private readonly string _connStr;

    public ExameCatalogoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public virtual async Task<IEnumerable<ExameCatalogoDto>> Buscar(string? busca, int limite = 30)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                e.id   AS Id,
                e.nome AS Nome,
                e.tipo AS Tipo
            FROM exame_catalogo e
            WHERE e.ativo = true
              AND (@Busca::text IS NULL
                   OR e.nome ILIKE '%' || @Busca || '%')
            ORDER BY e.nome
            LIMIT @Limite
            """;

        return await conn.QueryAsync<ExameCatalogoDto>(sql, new
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            Limite = limite
        });
    }
}
