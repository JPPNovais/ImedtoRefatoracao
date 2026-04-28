using Dapper;
using Imedto.Backend.Contracts.Unidades.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class UnidadeQueryRepository
{
    private readonly string _connectionString;

    public UnidadeQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<IEnumerable<UnidadeDto>> ListarPorEstabelecimento(long estabelecimentoId)
    {
        const string sql = """
            SELECT  u.id                  AS Id,
                    u.estabelecimento_id  AS EstabelecimentoId,
                    u.nome                AS Nome,
                    u.is_principal        AS IsPrincipal,
                    u.cep                 AS Cep,
                    u.logradouro          AS Logradouro,
                    u.numero              AS Numero,
                    u.complemento         AS Complemento,
                    u.bairro              AS Bairro,
                    u.cidade              AS Cidade,
                    u.estado              AS Estado,
                    u.telefone            AS Telefone,
                    u.ativo               AS Ativo,
                    u.criado_em           AS CriadoEm
            FROM    public.unidades_estabelecimento u
            WHERE   u.estabelecimento_id = @EstabelecimentoId
            ORDER BY u.is_principal DESC, u.nome
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<UnidadeDto>(sql, new { EstabelecimentoId = estabelecimentoId });
    }
}
