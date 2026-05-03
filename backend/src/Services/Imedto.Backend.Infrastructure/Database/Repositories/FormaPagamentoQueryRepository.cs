using Dapper;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class FormaPagamentoQueryRepository
{
    private readonly string _connStr;

    public FormaPagamentoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<FormaPagamentoDto>> Listar(
        long estabelecimentoId,
        bool? ativas,
        bool? padrao)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // SELECT minimizado (LGPD): estabelecimento_id e atualizada_em removidos.
        const string sql = """
            SELECT
                f.id                    AS Id,
                f.nome                  AS Nome,
                f.padrao                AS Padrao,
                f.ativo                 AS Ativo,
                f.criada_em             AS CriadaEm
            FROM formas_pagamento f
            WHERE f.estabelecimento_id = @EstabelecimentoId
              AND (@Ativas::bool IS NULL OR f.ativo  = @Ativas::bool)
              AND (@Padrao::bool IS NULL OR f.padrao = @Padrao::bool)
            ORDER BY f.nome
            """;

        return await conn.QueryAsync<FormaPagamentoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Ativas = ativas,
            Padrao = padrao
        });
    }
}
