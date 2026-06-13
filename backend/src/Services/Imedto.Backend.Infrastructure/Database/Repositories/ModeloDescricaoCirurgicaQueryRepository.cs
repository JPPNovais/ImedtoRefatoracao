using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloDescricaoCirurgicaQueryRepository
{
    private readonly string _connectionString;

    public ModeloDescricaoCirurgicaQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<IEnumerable<ModeloDescricaoCirurgicaDto>> Listar(long estabelecimentoId, bool apenasAtivos)
    {
        const string sql = """
            SELECT  id                  AS Id,
                    titulo              AS Titulo,
                    corpo               AS Corpo,
                    ativo               AS Ativo,
                    eh_padrao_sistema   AS EhPadraoSistema
            FROM    public.modelos_descricao_cirurgica
            WHERE   (eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId)
              AND   (@ApenasAtivos = false OR ativo = true)
            ORDER BY eh_padrao_sistema DESC, titulo
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<ModeloDescricaoCirurgicaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ApenasAtivos = apenasAtivos
        });
    }
}
