using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Migracao;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Read repository Dapper para queries admin de migração.
/// Singleton — sem dependências scoped.
/// LGPD: zero PII de paciente — apenas metadados do job e entidades mapeadas.
/// </summary>
public class MigracaoAdminQueryRepository
{
    private readonly string _connStr;

    public MigracaoAdminQueryRepository(AppReadConnectionString connStr)
    {
        _connStr = connStr.Value;
    }

    public virtual async Task<(List<MigracaoJobAdminDto> Itens, int Total)> ListarJobsAsync(
        long? estabelecimentoId,
        string? status,
        int pagina,
        int tamanho,
        DateTime? criadoDe = null,
        DateTime? criadoAte = null,
        string? onda = null,
        string? origem = null,
        CancellationToken ct = default)
    {
        var where = new List<string>();
        if (estabelecimentoId.HasValue) where.Add("j.estabelecimento_id = @EstId");
        if (!string.IsNullOrWhiteSpace(status)) where.Add("j.status = @Status");
        if (criadoDe.HasValue) where.Add("j.criado_em >= @CriadoDe");
        if (criadoAte.HasValue) where.Add("j.criado_em < @CriadoAte");
        if (onda == "onda1") where.Add("j.onda IS NULL");
        else if (onda == "onda2" || onda == "prontuario") where.Add("j.onda = 'prontuario'");
        if (!string.IsNullOrWhiteSpace(origem)) where.Add("j.origem ILIKE @Origem");

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

        var p = new
        {
            EstId    = estabelecimentoId,
            Status   = status,
            Tamanho  = tamanho,
            Offset   = (pagina - 1) * tamanho,
            CriadoDe = criadoDe,
            CriadoAte = criadoAte.HasValue ? criadoAte.Value.AddDays(1) : (DateTime?)null,
            Origem   = string.IsNullOrWhiteSpace(origem) ? null : $"%{origem}%",
        };

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
                t.nome                  AS NomeTemplate,
                j.motivo_falha          AS MotivoFalha
            FROM migracao_jobs j
            LEFT JOIN migracao_templates t
                ON t.id = j.template_origem_id
            WHERE j.id = @JobId
            """;

        const string sqlMapas = """
            SELECT
                m.id                    AS Id,
                m.entidade              AS Entidade,
                m.nome_bloco_origem     AS NomeBlocoOrigem,
                m.mapa_json             AS MapaJson,
                m.revisado_por_usuario_id AS RevisadoPorUsuarioId,
                m.revisado_em           AS RevisadoEm,
                m.criado_em             AS CriadoEm
            FROM migracao_mapas m
            WHERE m.migracao_job_id = @JobId
            ORDER BY m.entidade, m.nome_bloco_origem
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

    public virtual async Task<List<MigracaoJobEventoDto>> ListarEventosAsync(long jobId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT status_anterior AS StatusAnterior,
                   status_novo     AS StatusNovo,
                   usuario_id      AS UsuarioId,
                   criado_em       AS CriadoEm
            FROM migracao_job_eventos
            WHERE migracao_job_id = @JobId
            ORDER BY criado_em ASC
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return (await conn.QueryAsync<MigracaoJobEventoDto>(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: ct))).AsList();
    }

    public virtual async Task<ProgressoMigracaoResult> ObterProgressoAsync(long jobId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT entidade, status, COUNT(*)::int AS total
            FROM migracao_registros
            WHERE migracao_job_id = @JobId
            GROUP BY entidade, status
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var linhas = (await conn.QueryAsync<(string Entidade, string Status, int Total)>(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: ct))).AsList();

        var porEntidade = linhas
            .GroupBy(l => l.Entidade)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var totalGeral  = g.Sum(l => l.Total);
                    var pendentes   = g.FirstOrDefault(l => l.Status == "pendente").Total;
                    var criados     = g.FirstOrDefault(l => l.Status == "importado_criado").Total;
                    var atualizados = g.FirstOrDefault(l => l.Status == "importado_atualizado").Total;
                    var rejeitados  = g.FirstOrDefault(l => l.Status == "rejeitado").Total;
                    var pulados     = g.FirstOrDefault(l => l.Status == "pulado").Total;
                    var processados = totalGeral - pendentes;
                    var pct         = totalGeral > 0 ? (int)Math.Round(processados * 100.0 / totalGeral) : 0;
                    return new ProgressoEntidadeDto
                    {
                        Total       = totalGeral,
                        Pendentes   = pendentes,
                        Criados     = criados,
                        Atualizados = atualizados,
                        Rejeitados  = rejeitados,
                        Pulados     = pulados,
                        Percentual  = pct,
                    };
                });

        var totalTodos    = porEntidade.Values.Sum(e => e.Total);
        var pendentesTodos = porEntidade.Values.Sum(e => e.Pendentes);
        var pctAgregado   = totalTodos > 0 ? (int)Math.Round((totalTodos - pendentesTodos) * 100.0 / totalTodos) : 0;

        return new ProgressoMigracaoResult
        {
            PorEntidade        = porEntidade,
            PercentualAgregado = pctAgregado,
        };
    }
}
