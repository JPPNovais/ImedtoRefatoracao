using Dapper;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class Cid10QueryRepository
{
    private readonly string _connStr;

    public Cid10QueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public virtual async Task<IEnumerable<Cid10Dto>> Buscar(string? busca, int limite = 20)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                c.codigo    AS Codigo,
                c.descricao AS Descricao,
                c.categoria AS Categoria
            FROM cid10 c
            WHERE (@Busca::text IS NULL
                   OR c.codigo    ILIKE '%' || @Busca || '%'
                   OR c.descricao ILIKE '%' || @Busca || '%')
            ORDER BY c.descricao
            LIMIT @Limite
            """;

        return await conn.QueryAsync<Cid10Dto>(sql, new
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            Limite = limite
        });
    }
}
