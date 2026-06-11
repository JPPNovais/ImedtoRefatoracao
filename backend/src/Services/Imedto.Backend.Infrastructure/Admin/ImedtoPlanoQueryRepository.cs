using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Planos.Queries.Results;
using Imedto.Backend.Infrastructure;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Read repository (Dapper) para projeções de ImedtoPlano em DTO admin.
/// Singleton — sem state de instância, apenas connection string.
/// </summary>
public class ImedtoPlanoQueryRepository
{
    private readonly string _connectionString;

    public ImedtoPlanoQueryRepository(AppReadConnectionString conn)
    {
        _connectionString = conn.Value;
    }

    public async Task<ListarPlanosAdminResult> ListarAsync(
        bool? ativo,
        string? busca,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        const string sqlBase = """
            FROM imedto_planos p
            WHERE (@Ativo IS NULL OR p.ativo = @Ativo)
              AND (@Busca IS NULL OR p.nome ILIKE '%' || @Busca || '%')
            """;

        var sqlCount = $"SELECT COUNT(*) {sqlBase}";
        var sqlItens = $"""
            SELECT
                p.id             AS Id,
                p.nome           AS Nome,
                p.descricao_curta AS DescricaoCurta,
                p.preco_mensal_centavos AS PrecoMensalCentavos,
                p.gratuito       AS Gratuito,
                p.ativo          AS Ativo,
                p.limites_json::text AS LimitesJson,
                p.features_json::text AS FeaturesJson,
                p.criado_em      AS CriadoEm,
                p.atualizado_em  AS AtualizadoEm
            {sqlBase}
            ORDER BY p.gratuito DESC, p.nome ASC
            LIMIT @Tamanho OFFSET @Offset
            """;

        var param = new
        {
            Ativo = ativo,
            Busca = busca,
            Tamanho = tamanho,
            Offset = (pagina - 1) * tamanho
        };

        await using var conn = new NpgsqlConnection(_connectionString);

        var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sqlCount, param, cancellationToken: ct));
        var itens = await conn.QueryAsync<PlanoAdminDto>(new CommandDefinition(sqlItens, param, cancellationToken: ct));

        return new ListarPlanosAdminResult(itens.ToList(), total, pagina, tamanho);
    }

    public async Task<PlanoAdminDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                p.id             AS Id,
                p.nome           AS Nome,
                p.descricao_curta AS DescricaoCurta,
                p.preco_mensal_centavos AS PrecoMensalCentavos,
                p.gratuito       AS Gratuito,
                p.ativo          AS Ativo,
                p.limites_json::text AS LimitesJson,
                p.features_json::text AS FeaturesJson,
                p.criado_em      AS CriadoEm,
                p.atualizado_em  AS AtualizadoEm
            FROM imedto_planos p
            WHERE p.id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<PlanoAdminDto>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
