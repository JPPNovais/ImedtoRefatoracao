using Dapper;
using Imedto.Backend.Contracts.Admin.Admins.Queries.Results;
using Imedto.Backend.Infrastructure.Database;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Query repository Dapper para leituras de admins. Singleton — sem state per-request.
/// Sem filtro de estabelecimento_id: tabela imedto_admins é global por design.
/// </summary>
public class AdminQueryRepository
{
    private readonly string _connectionString;

    public AdminQueryRepository(AppReadConnectionString connection)
        => _connectionString = connection.Value;

    public async Task<(IEnumerable<AdminListItemDto> Items, int Total)> ListarAdmins(
        string busca,
        int pagina,
        int tamanho)
    {
        // CA37: busca case-insensitive em nome e email
        const string sqlCount = """
            SELECT COUNT(*)
            FROM   public.imedto_admins
            WHERE  (@Busca IS NULL
                    OR nome   ILIKE '%' || @Busca || '%'
                    OR email  ILIKE '%' || @Busca || '%')
            """;

        const string sqlItems = """
            SELECT  id                  AS Id,
                    email               AS Email,
                    nome                AS Nome,
                    ativo               AS Ativo,
                    force_password_reset AS ForcePasswordReset,
                    criado_em           AS CriadoEm,
                    ultimo_login_em     AS UltimoLoginEm
            FROM    public.imedto_admins
            WHERE   (@Busca IS NULL
                     OR nome   ILIKE '%' || @Busca || '%'
                     OR email  ILIKE '%' || @Busca || '%')
            ORDER BY nome ASC
            LIMIT  @Tamanho
            OFFSET @Offset
            """;

        var buscaNorm = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
        var offset = (pagina - 1) * tamanho;

        await using var conn = new NpgsqlConnection(_connectionString);
        var total = await conn.ExecuteScalarAsync<int>(sqlCount, new { Busca = buscaNorm });
        var items = await conn.QueryAsync<AdminListItemDto>(sqlItems, new
        {
            Busca = buscaNorm,
            Tamanho = tamanho,
            Offset = offset
        });

        return (items, total);
    }

    public async Task<AdminDetalheDto?> ObterAdmin(Guid id)
    {
        const string sql = """
            SELECT  id                       AS Id,
                    email                    AS Email,
                    nome                     AS Nome,
                    ativo                    AS Ativo,
                    force_password_reset     AS ForcePasswordReset,
                    criado_em                AS CriadoEm,
                    criado_por_admin_id      AS CriadoPor,
                    desativado_em            AS DesativadoEm,
                    desativado_por_admin_id  AS DesativadoPor,
                    ultimo_login_em          AS UltimoLoginEm
            FROM    public.imedto_admins
            WHERE   id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<AdminDetalheDto>(sql, new { Id = id });
    }
}
