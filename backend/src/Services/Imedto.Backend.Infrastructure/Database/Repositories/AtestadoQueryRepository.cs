using Dapper;
using Imedto.Backend.Contracts.Atestados.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side de atestados (Dapper). Sempre filtra por <c>estabelecimento_id</c>
/// e <c>deletado_em IS NULL</c>.
/// </summary>
public interface IAtestadoQueryRepository
{
    Task<IReadOnlyList<AtestadoDto>> ListarDoPaciente(long pacienteId, long estabelecimentoId);
    Task<AtestadoDto?> ObterPorId(long atestadoId, long estabelecimentoId);
    Task<IReadOnlyList<ModeloAtestadoDto>> ListarModelos(long estabelecimentoId);
}

public class AtestadoQueryRepository : IAtestadoQueryRepository
{
    private readonly string _connStr;

    public AtestadoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IReadOnlyList<AtestadoDto>> ListarDoPaciente(long pacienteId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  a.id                  AS Id,
                    a.paciente_id         AS PacienteId,
                    a.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    a.tipo                AS Tipo,
                    a.dias_afastamento    AS DiasAfastamento,
                    a.cid10               AS Cid10,
                    a.conteudo            AS Conteudo,
                    a.criado_em           AS CriadoEm
            FROM    public.atestados a
            LEFT JOIN public.usuarios u ON u.id = a.profissional_usuario_id
            WHERE   a.paciente_id = @PacienteId
              AND   a.estabelecimento_id = @EstabelecimentoId
              AND   a.deletado_em IS NULL
            ORDER BY a.criado_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var itens = await conn.QueryAsync<AtestadoDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
        });
        return itens.ToList();
    }

    public async Task<AtestadoDto?> ObterPorId(long atestadoId, long estabelecimentoId)
    {
        const string sql = """
            SELECT  a.id                  AS Id,
                    a.paciente_id         AS PacienteId,
                    a.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    a.tipo                AS Tipo,
                    a.dias_afastamento    AS DiasAfastamento,
                    a.cid10               AS Cid10,
                    a.conteudo            AS Conteudo,
                    a.criado_em           AS CriadoEm
            FROM    public.atestados a
            LEFT JOIN public.usuarios u ON u.id = a.profissional_usuario_id
            WHERE   a.id = @Id
              AND   a.estabelecimento_id = @EstabelecimentoId
              AND   a.deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<AtestadoDto>(sql, new
        {
            Id = atestadoId,
            EstabelecimentoId = estabelecimentoId,
        });
    }

    public async Task<IReadOnlyList<ModeloAtestadoDto>> ListarModelos(long estabelecimentoId)
    {
        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    tipo            AS Tipo,
                    conteudo        AS Conteudo,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.modelos_atestado
            WHERE   estabelecimento_id = @EstabelecimentoId
            ORDER BY nome
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var itens = await conn.QueryAsync<ModeloAtestadoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
        });
        return itens.ToList();
    }
}
