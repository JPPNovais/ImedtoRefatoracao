using Dapper;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class CategoriaFinanceiraQueryRepository
{
    private readonly string _connStr;

    public CategoriaFinanceiraQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<CategoriaFinanceiraDto>> Listar(
        long estabelecimentoId,
        string? tipo,
        bool? ativas,
        bool? padrao)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                c.id                    AS Id,
                c.estabelecimento_id    AS EstabelecimentoId,
                c.nome                  AS Nome,
                c.tipo                  AS Tipo,
                c.padrao                AS Padrao,
                c.ativo                 AS Ativo,
                c.criada_em             AS CriadaEm,
                c.atualizada_em         AS AtualizadaEm
            FROM categorias_financeiras c
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND (@Tipo::text   IS NULL OR c.tipo   = @Tipo::text)
              AND (@Ativas::bool IS NULL OR c.ativo  = @Ativas::bool)
              AND (@Padrao::bool IS NULL OR c.padrao = @Padrao::bool)
            ORDER BY c.tipo, c.nome
            """;

        return await conn.QueryAsync<CategoriaFinanceiraDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Ativas = ativas,
            Padrao = padrao
        });
    }
}
