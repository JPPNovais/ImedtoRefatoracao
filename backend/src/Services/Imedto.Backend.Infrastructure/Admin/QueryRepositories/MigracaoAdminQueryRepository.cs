using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Migracao;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Read repository Dapper para queries admin de migração.
/// Singleton — sem dependências scoped.
/// LGPD: zero PII de paciente — apenas metadados do job e entidades mapeadas.
/// </summary>
public sealed class MigracaoAdminQueryRepository
{
    private readonly string _connStr;

    public MigracaoAdminQueryRepository(AppReadConnectionString connStr)
    {
        _connStr = connStr.Value;
    }

    public async Task<(List<MigracaoJobAdminDto> Itens, int Total)> ListarJobsAsync(
        long? estabelecimentoId,
        string? status,
        int pagina,
        int tamanho,
        CancellationToken ct = default)
    {
        var where = new List<string>();
        if (estabelecimentoId.HasValue) where.Add("j.estabelecimento_id = @EstId");
        if (!string.IsNullOrWhiteSpace(status)) where.Add("j.status = @Status");

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;

        var sqlCount = $"SELECT COUNT(*)::int FROM migracao_jobs j {whereClause}";
        var sqlItens = $"""
            SELECT
                j.id             AS Id,
                j.estabelecimento_id AS EstabelecimentoId,
                j.status         AS Status,
                j.origem         AS Origem,
                j.criado_por_usuario_id AS CriadoPorUsuarioId,
                j.criado_em      AS CriadoEm,
                j.atualizado_em  AS AtualizadoEm
            FROM migracao_jobs j
            {whereClause}
            ORDER BY j.criado_em DESC
            LIMIT @Tamanho OFFSET @Offset
            """;

        var p = new { EstId = estabelecimentoId, Status = status, Tamanho = tamanho, Offset = (pagina - 1) * tamanho };

        await using var conn = new NpgsqlConnection(_connStr);
        var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sqlCount, p, cancellationToken: ct));
        var itens = (await conn.QueryAsync<MigracaoJobAdminDto>(
            new CommandDefinition(sqlItens, p, cancellationToken: ct))).AsList();

        return (itens, total);
    }

    public async Task<MigracaoJobAdminDto?> ObterJobAsync(long jobId, CancellationToken ct = default)
    {
        const string sqlJob = """
            SELECT
                j.id                    AS Id,
                j.estabelecimento_id    AS EstabelecimentoId,
                j.status                AS Status,
                j.origem                AS Origem,
                j.criado_por_usuario_id AS CriadoPorUsuarioId,
                j.criado_em             AS CriadoEm,
                j.atualizado_em         AS AtualizadoEm,
                j.template_origem_id    AS TemplateOrigemId,
                t.nome                  AS NomeTemplate
            FROM migracao_jobs j
            LEFT JOIN migracao_templates t
                ON t.id = j.template_origem_id
            WHERE j.id = @JobId
            """;

        const string sqlMapas = """
            SELECT
                m.id             AS Id,
                m.entidade       AS Entidade,
                m.mapa_json      AS MapaJson,
                m.revisado_por_usuario_id AS RevisadoPorUsuarioId,
                m.revisado_em    AS RevisadoEm,
                m.criado_em      AS CriadoEm
            FROM migracao_mapas m
            WHERE m.migracao_job_id = @JobId
            ORDER BY m.entidade
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var job = await conn.QuerySingleOrDefaultAsync<MigracaoJobAdminDto>(
            new CommandDefinition(sqlJob, new { JobId = jobId }, cancellationToken: ct));

        if (job is null) return null;

        var mapas = (await conn.QueryAsync<MigracaoMapaDto>(
            new CommandDefinition(sqlMapas, new { JobId = jobId }, cancellationToken: ct))).AsList();

        job.Mapas = mapas;
        return job;
    }
}
