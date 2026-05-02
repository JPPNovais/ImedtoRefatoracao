using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class ModeloProntuarioQueryRepository
{
    private readonly string _connectionString;

    public ModeloProntuarioQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    /// <summary>UNION dos modelos padrão-sistema + do estabelecimento informado.</summary>
    public async Task<IEnumerable<ModeloProntuarioDto>> ListarDisponiveis(long estabelecimentoId, bool apenasAtivos)
    {
        const string sql = """
            SELECT  id                  AS Id,
                    nome                AS Nome,
                    descricao           AS Descricao,
                    estrutura           AS Estrutura,
                    eh_padrao_sistema   AS EhPadraoSistema,
                    ativo               AS Ativo,
                    criado_em           AS CriadoEm
            FROM    public.modelo_de_prontuario
            WHERE   (eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId)
              AND   (@ApenasAtivos = false OR ativo = true)
            ORDER BY eh_padrao_sistema DESC, nome
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<ModeloProntuarioDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ApenasAtivos = apenasAtivos
        });
    }

    public async Task<ModeloProntuarioDto> ObterVisivelPara(long modeloId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  id                  AS Id,
                    nome                AS Nome,
                    descricao           AS Descricao,
                    estrutura           AS Estrutura,
                    eh_padrao_sistema   AS EhPadraoSistema,
                    ativo               AS Ativo,
                    criado_em           AS CriadoEm
            FROM    public.modelo_de_prontuario
            WHERE   id = @ModeloId
              AND   (eh_padrao_sistema = true OR estabelecimento_id = @EstabelecimentoId)
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<ModeloProntuarioDto>(sql, new
        {
            ModeloId = modeloId,
            EstabelecimentoId = estabelecimentoId
        });
    }
}
