using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Catalogos.Regioes.Queries.Results;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Repositório de leitura (Dapper) para regiões anatômicas globais.
/// Singleton — stateless.
/// </summary>
public class ImedtoRegiaoAnatomicaGlobalQueryRepository
{
    private readonly string _connStr;

    public ImedtoRegiaoAnatomicaGlobalQueryRepository(AppReadConnectionString connStr)
        => _connStr = connStr.Value;

    public async Task<(IReadOnlyList<RegiaoGlobalListaItemDto> Itens, int Total)> ListarAsync(
        bool incluirInativos,
        string? busca,
        string? sistemaCorporal,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var filtroAtivo = incluirInativos ? "" : "AND ativo = true";
        var filtroBusca = string.IsNullOrWhiteSpace(busca) ? "" : "AND LOWER(nome) LIKE @busca";
        var filtroSistema = string.IsNullOrWhiteSpace(sistemaCorporal) ? "" : "AND sistema_corporal = @sistemaCorporal";

        var sql = $"""
            SELECT
                id AS Id,
                nome AS Nome,
                sinonimos AS Sinonimos,
                sistema_corporal AS SistemaCorporal,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM imedto_regiao_anatomica_global
            WHERE 1=1 {filtroAtivo} {filtroBusca} {filtroSistema}
            ORDER BY nome
            LIMIT @limit OFFSET @offset;

            SELECT COUNT(*)
            FROM imedto_regiao_anatomica_global
            WHERE 1=1 {filtroAtivo} {filtroBusca} {filtroSistema};
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var p = new { busca = $"%{busca?.ToLowerInvariant()}%", sistemaCorporal, limit = tamanhoPagina, offset };
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
        var itens = (await multi.ReadAsync<RegiaoGlobalListaItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    public async Task<RegiaoGlobalDetalheDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                nome AS Nome,
                sinonimos AS Sinonimos,
                sistema_corporal AS SistemaCorporal,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM imedto_regiao_anatomica_global
            WHERE id = @id
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<RegiaoGlobalDetalheDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }
}
