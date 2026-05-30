using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Queries.Results;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Repositório de leitura (Dapper) para variáveis pool globais.
/// Singleton — stateless.
/// </summary>
public class ImedtoVariavelPoolGlobalQueryRepository
{
    private readonly string _connStr;

    public ImedtoVariavelPoolGlobalQueryRepository(AppReadConnectionString connStr)
        => _connStr = connStr.Value;

    public async Task<(IReadOnlyList<VariavelGlobalListaItemDto> Itens, int Total)> ListarAsync(
        bool incluirInativos,
        string? busca,
        string? tipo,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var filtroAtivo = incluirInativos ? "" : "AND ativo = true";
        var filtroBusca = string.IsNullOrWhiteSpace(busca) ? "" : "AND LOWER(nome) LIKE @busca";
        var filtroTipo = string.IsNullOrWhiteSpace(tipo) ? "" : "AND tipo = @tipo";

        var sql = $"""
            SELECT
                id AS Id,
                nome AS Nome,
                tipo AS Tipo,
                descricao AS Descricao,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM imedto_variavel_pool_global
            WHERE 1=1 {filtroAtivo} {filtroBusca} {filtroTipo}
            ORDER BY nome
            LIMIT @limit OFFSET @offset;

            SELECT COUNT(*)
            FROM imedto_variavel_pool_global
            WHERE 1=1 {filtroAtivo} {filtroBusca} {filtroTipo};
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var p = new { busca = $"%{busca?.ToLowerInvariant()}%", tipo, limit = tamanhoPagina, offset };
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
        var itens = (await multi.ReadAsync<VariavelGlobalListaItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    public async Task<VariavelGlobalDetalheDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                nome AS Nome,
                tipo AS Tipo,
                valores_json AS ValoresJson,
                descricao AS Descricao,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm,
                criado_por_admin_id AS CriadoPorAdminId,
                atualizado_por_admin_id AS AtualizadoPorAdminId
            FROM imedto_variavel_pool_global
            WHERE id = @id
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<VariavelGlobalDetalheDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }
}
