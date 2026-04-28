using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class VariavelPoolQueryRepository
{
    private readonly string _connectionString;

    public VariavelPoolQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<IEnumerable<VariavelPoolDto>> Listar(long estabelecimentoId, string tipo, bool apenasAtivos)
    {
        const string sql = """
            SELECT  id                  AS Id,
                    estabelecimento_id  AS EstabelecimentoId,
                    tipo                AS Tipo,
                    nome                AS Nome,
                    ativo               AS Ativo,
                    eh_padrao_sistema   AS EhPadraoSistema
            FROM    public.prontuario_variaveis_pool
            WHERE   (eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId)
              AND   (@Tipo IS NULL OR tipo = @Tipo)
              AND   (@ApenasAtivos = false OR ativo = true)
            ORDER BY eh_padrao_sistema DESC, nome
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<VariavelPoolDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            ApenasAtivos = apenasAtivos
        });
    }
}
