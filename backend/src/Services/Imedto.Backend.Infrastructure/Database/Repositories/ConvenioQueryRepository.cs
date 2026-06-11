using Dapper;
using Imedto.Backend.Contracts.Convenios.Queries.Results;
using Npgsql;

// Tipos internos para mapeamento Dapper

namespace Imedto.Backend.Infrastructure.Database.Repositories;

// Tipos internos para mapeamento Dapper (evitar dynamic)
file sealed class ConvenioSelectRow { public long Id { get; init; } public string Nome { get; init; } = ""; }
file sealed class ConvenioPlanoSelectRow { public long Id { get; init; } public long ConvenioId { get; init; } public string Nome { get; init; } = ""; public bool Ativo { get; init; } }

/// <summary>
/// Repositório Dapper para leituras de convênio (singleton).
/// Todos os métodos filtram por estabelecimento_id (multi-tenant falha-fechada).
/// </summary>
public class ConvenioQueryRepository
{
    private readonly string _connStr;

    public ConvenioQueryRepository(AppReadConnectionString conn)
        => _connStr = conn.Value;

    public async Task<IReadOnlyList<ConvenioListadoDto>> ListarConvenios(long estabelecimentoId, bool apenasAtivos)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlBase = """
            SELECT
                c.id            AS Id,
                c.nome          AS Nome,
                c.registro_ans  AS RegistroAns,
                c.ativo         AS Ativo,
                COUNT(p.id)     AS TotalPlanos
            FROM convenios c
            LEFT JOIN convenio_planos p ON p.convenio_id = c.id AND p.ativo = TRUE
            WHERE c.estabelecimento_id = @EstabelecimentoId
            """;

        var sql = apenasAtivos
            ? sqlBase + " AND c.ativo = TRUE GROUP BY c.id ORDER BY c.nome"
            : sqlBase + " GROUP BY c.id ORDER BY c.nome";

        var resultado = await conn.QueryAsync<ConvenioListadoDto>(sql, new { EstabelecimentoId = estabelecimentoId });
        return resultado.ToList();
    }

    public async Task<ConvenioDetalheDto?> ObterDetalhe(long convenioId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlConvenio = """
            SELECT id AS Id, nome AS Nome, registro_ans AS RegistroAns, ativo AS Ativo
            FROM convenios
            WHERE id = @Id AND estabelecimento_id = @EstabelecimentoId
            LIMIT 1
            """;

        const string sqlPlanos = """
            SELECT id AS Id, nome AS Nome, ativo AS Ativo
            FROM convenio_planos
            WHERE convenio_id = @Id
            ORDER BY nome
            """;

        await using var multi = await conn.QueryMultipleAsync(
            sqlConvenio + ";\n" + sqlPlanos,
            new { Id = convenioId, EstabelecimentoId = estabelecimentoId });

        var convenioRow = await multi.ReadSingleOrDefaultAsync<dynamic>();
        if (convenioRow is null) return null;

        var planos = (await multi.ReadAsync<ConvenioPlanoDto>()).ToList();

        return new ConvenioDetalheDto(
            convenioRow.Id,
            convenioRow.Nome,
            convenioRow.RegistroAns,
            convenioRow.Ativo,
            planos);
    }

    /// <summary>Convênios ativos com seus planos ativos — para select do check-in e cadastro de carteirinha.</summary>
    public async Task<IReadOnlyList<ConvenioSelectDto>> ListarAtivosComPlanos(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlConvenios = """
            SELECT id AS Id, nome AS Nome
            FROM convenios
            WHERE estabelecimento_id = @EstabelecimentoId AND ativo = TRUE
            ORDER BY nome
            """;

        const string sqlPlanos = """
            SELECT cp.id AS Id, cp.convenio_id AS ConvenioId, cp.nome AS Nome, cp.ativo AS Ativo
            FROM convenio_planos cp
            JOIN convenios c ON c.id = cp.convenio_id
            WHERE c.estabelecimento_id = @EstabelecimentoId AND c.ativo = TRUE AND cp.ativo = TRUE
            ORDER BY cp.convenio_id, cp.nome
            """;

        await using var multi = await conn.QueryMultipleAsync(
            sqlConvenios + ";\n" + sqlPlanos,
            new { EstabelecimentoId = estabelecimentoId });

        var convenios = (await multi.ReadAsync<ConvenioSelectRow>()).ToList();
        var planos = (await multi.ReadAsync<ConvenioPlanoSelectRow>()).ToList();

        return convenios.Select(c =>
        {
            var planosDoConvenio = planos
                .Where(p => p.ConvenioId == c.Id)
                .Select(p => new ConvenioPlanoDto(p.Id, p.Nome, p.Ativo))
                .ToList();
            return new ConvenioSelectDto(c.Id, c.Nome, planosDoConvenio);
        }).ToList();
    }
}
