using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Assinaturas.Queries.Results;
using Imedto.Backend.Infrastructure;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Read repository (Dapper) para projeções de ImedtoAssinatura em DTO admin.
/// Singleton — sem state de instância, apenas connection string.
/// </summary>
public class ImedtoAssinaturaQueryRepository
{
    private readonly string _connectionString;

    public ImedtoAssinaturaQueryRepository(AppReadConnectionString conn)
    {
        _connectionString = conn.Value;
    }

    public async Task<IReadOnlyList<AssinaturaAdminDto>> ListarHistoricoAsync(
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                a.id                AS Id,
                a.estabelecimento_id AS EstabelecimentoId,
                a.plano_id          AS PlanoId,
                p.nome              AS PlanoNome,
                p.gratuito          AS PlanoGratuito,
                a.iniciada_em       AS IniciadaEm,
                a.fim_em            AS FimEm,
                a.gratuita          AS Gratuita,
                a.motivo            AS Motivo,
                a.criada_em         AS CriadaEm,
                (a.fim_em IS NULL)  AS Vigente
            FROM imedto_assinaturas a
            INNER JOIN imedto_planos p ON p.id = a.plano_id
            WHERE a.estabelecimento_id = @EstabelecimentoId
            ORDER BY a.iniciada_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.QueryAsync<AssinaturaAdminDto>(
            new CommandDefinition(sql, new { EstabelecimentoId = estabelecimentoId }, cancellationToken: ct));

        return result.ToList();
    }
}
