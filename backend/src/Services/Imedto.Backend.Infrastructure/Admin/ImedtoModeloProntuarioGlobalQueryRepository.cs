using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Queries.Results;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Repositório de leitura (Dapper) para modelos de prontuário globais.
/// Singleton — stateless, connection string imutável.
/// </summary>
public class ImedtoModeloProntuarioGlobalQueryRepository
{
    private readonly string _connStr;

    public ImedtoModeloProntuarioGlobalQueryRepository(AppReadConnectionString connStr)
        => _connStr = connStr.Value;

    public async Task<(IReadOnlyList<ModeloGlobalListaItemDto> Itens, int Total)> ListarAsync(
        bool incluirInativos,
        string? busca,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var filtroAtivo = incluirInativos ? "" : "AND ativo = true";
        var filtroBusca = string.IsNullOrWhiteSpace(busca) ? "" : "AND LOWER(nome) LIKE @busca";

        var sql = $"""
            SELECT
                id AS Id,
                nome AS Nome,
                descricao AS Descricao,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm
            FROM imedto_modelo_prontuario_global
            WHERE 1=1 {filtroAtivo} {filtroBusca}
            ORDER BY nome
            LIMIT @limit OFFSET @offset;

            SELECT COUNT(*)
            FROM imedto_modelo_prontuario_global
            WHERE 1=1 {filtroAtivo} {filtroBusca};
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var p = new { busca = $"%{busca?.ToLowerInvariant()}%", limit = tamanhoPagina, offset };
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, p, cancellationToken: ct));
        var itens = (await multi.ReadAsync<ModeloGlobalListaItemDto>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    public async Task<ModeloGlobalDetalheDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                nome AS Nome,
                descricao AS Descricao,
                conteudo_json AS ConteudoJson,
                ativo AS Ativo,
                criado_em AS CriadoEm,
                atualizado_em AS AtualizadoEm,
                criado_por_admin_id AS CriadoPorAdminId,
                atualizado_por_admin_id AS AtualizadoPorAdminId
            FROM imedto_modelo_prontuario_global
            WHERE id = @id
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<ModeloGlobalDetalheDto>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }
}
