using Dapper;
using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ProcedimentoCatalogoQueryRepository
{
    private readonly string _connStr;

    public ProcedimentoCatalogoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<ProcedimentoCatalogoDto>> Buscar(string? termo, string? origem, bool? ativo, int limit = 20)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                p.id        AS Id,
                p.codigo    AS Codigo,
                p.nome      AS Nome,
                p.origem    AS Origem,
                p.capitulo  AS Capitulo
            FROM catalogo_procedimentos p
            WHERE (@Termo::text IS NULL
                   OR p.codigo ILIKE '%' || @Termo || '%'
                   OR p.nome   ILIKE '%' || @Termo || '%')
              AND (@Origem::text IS NULL OR p.origem = @Origem)
              AND (@Ativo::boolean IS NULL OR p.ativo = @Ativo)
            ORDER BY p.nome
            LIMIT @Limit
            """;

        return await conn.QueryAsync<ProcedimentoCatalogoDto>(sql, new
        {
            Termo = string.IsNullOrWhiteSpace(termo) ? null : termo.Trim(),
            Origem = string.IsNullOrWhiteSpace(origem) ? null : origem.Trim(),
            Ativo = ativo,
            Limit = limit
        });
    }

    public async Task<ProcedimentoCatalogoDto?> ObterPorCodigo(string codigo)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                p.id        AS Id,
                p.codigo    AS Codigo,
                p.nome      AS Nome,
                p.origem    AS Origem,
                p.capitulo  AS Capitulo
            FROM catalogo_procedimentos p
            WHERE p.codigo = @Codigo
            LIMIT 1
            """;

        return await conn.QueryFirstOrDefaultAsync<ProcedimentoCatalogoDto>(sql, new { Codigo = codigo.Trim() });
    }
}
