using Dapper;
using Imedto.Backend.Contracts.Salas.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class SalaQueryRepository
{
    private readonly string _connectionString;

    public SalaQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<IEnumerable<SalaDto>> ListarPorEstabelecimento(long estabelecimentoId, bool apenasAtivas = false)
    {
        const string sql = """
            SELECT  s.id                AS Id,
                    s.estabelecimento_id AS EstabelecimentoId,
                    s.unidade_id         AS UnidadeId,
                    u.nome               AS UnidadeNome,
                    s.tipo_sala_id       AS TipoSalaId,
                    t.nome               AS TipoSalaNome,
                    s.nome               AS Nome,
                    s.descricao          AS Descricao,
                    s.ativo              AS Ativo,
                    s.criado_em          AS CriadoEm
            FROM    public.sala_atendimento s
            JOIN    public.unidades_estabelecimento u ON u.id = s.unidade_id
            LEFT JOIN public.tipo_sala_atendimento t ON t.id = s.tipo_sala_id
            WHERE   s.estabelecimento_id = @EstabelecimentoId
              AND   (@ApenasAtivas = false OR s.ativo = true)
            ORDER BY u.nome, s.nome
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<SalaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ApenasAtivas = apenasAtivas,
        });
    }

    public async Task<IEnumerable<TipoSalaDto>> ListarTipos()
    {
        const string sql = """
            SELECT id AS Id, nome AS Nome, descricao AS Descricao
            FROM public.tipo_sala_atendimento
            ORDER BY nome
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<TipoSalaDto>(sql);
    }
}
