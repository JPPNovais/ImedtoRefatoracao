using Dapper;
using Imedto.Backend.Contracts.Admin.Configs.Queries;
using Imedto.Backend.Contracts.Admin.Configs.Queries.Results;
using Imedto.Backend.Infrastructure;

namespace Imedto.Backend.Application.Admin.Configs;

/// <summary>
/// Retorna todas as configurações globais agrupadas por seção.
/// Singleton — leitura pura via Dapper sem estado mutável.
/// </summary>
public class ListarConfigsAdminQueryHandler
{
    private readonly string _connStr;

    public ListarConfigsAdminQueryHandler(AppReadConnectionString connStr)
    {
        _connStr = connStr.Value;
    }

    public async Task<IReadOnlyList<SecaoConfigsDto>> Handle(
        ListarConfigsAdminQuery _,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                chave      AS Chave,
                valor      AS Valor,
                tipo       AS Tipo,
                secao      AS Secao,
                descricao  AS Descricao,
                atualizado_em AS AtualizadoEm,
                atualizado_por_admin_id AS AtualizadoPorAdminId
            FROM imedto_config
            ORDER BY secao, chave
            """;

        await using var conn = new Npgsql.NpgsqlConnection(_connStr);
        var rows = await conn.QueryAsync<ConfigAdminDto>(new CommandDefinition(sql, cancellationToken: ct));

        return rows
            .GroupBy(r => r.Secao ?? "Sem seção")
            .Select(g => new SecaoConfigsDto(g.Key, g.ToList()))
            .OrderBy(s => s.Secao)
            .ToList();
    }
}
