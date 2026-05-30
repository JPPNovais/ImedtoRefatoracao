using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Dashboard.Queries.Results;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Read repository (Dapper) para as queries do dashboard admin.
/// Singleton — sem dependências scoped.
/// Zero joins em pacientes/prontuários — só metadados de tenant/admin (CA20).
/// Leitura do dashboard não gera audit (Wave 1 CA16).
/// </summary>
public interface IDashboardAdminQueryRepository
{
    Task<KpisDashboardDto> ObterKpisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CrescimentoMensalPontoDto>> ObterCrescimentoMensalAsync(CancellationToken ct = default);
    Task<AlertasDashboardDto> ObterAlertasAsync(CancellationToken ct = default);
    Task<AuditLogPaginadoDto> ListarAuditLogAsync(
        string? acao, Guid? adminId, string periodo, int pagina, int tamanhoPagina,
        CancellationToken ct = default);
}

public class DashboardAdminQueryRepository : IDashboardAdminQueryRepository
{
    private readonly string _connStr;

    public DashboardAdminQueryRepository(AppReadConnectionString connStr)
    {
        _connStr = connStr.Value;
    }

    /// <summary>
    /// KPIs: contagens de estabelecimentos, admins, trials e assinaturas.
    /// Query única com subqueries para evitar múltiplas roundtrips.
    /// </summary>
    public async Task<KpisDashboardDto> ObterKpisAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*)::int FROM public.estabelecimentos WHERE status = 'Ativo')
                    AS EstabelecimentosAtivos,
                (SELECT COUNT(*)::int FROM public.estabelecimentos WHERE status = 'Inativo')
                    AS EstabelecimentosInativos,
                (SELECT COUNT(*)::int FROM public.imedto_admins WHERE ativo = TRUE)
                    AS AdminsAtivos,
                (SELECT COUNT(*)::int
                 FROM public.imedto_assinaturas
                 WHERE gratuita = TRUE AND (fim_em IS NULL OR fim_em > NOW()))
                    AS TrialsEmAndamento,
                (SELECT COUNT(*)::int
                 FROM public.imedto_assinaturas
                 WHERE gratuita = TRUE
                   AND fim_em BETWEEN NOW() AND NOW() + INTERVAL '7 days')
                    AS TrialsExpirandoEm7Dias,
                (SELECT COUNT(*)::int
                 FROM public.imedto_assinaturas WHERE fim_em IS NULL)
                    AS AssinaturasVigentes,
                (SELECT COUNT(*)::int
                 FROM public.imedto_assinaturas WHERE fim_em IS NULL AND gratuita = TRUE)
                    AS AssinaturasGratuitas
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleAsync<KpisDashboardDto>(
            new CommandDefinition(sql, cancellationToken: ct));
    }

    /// <summary>
    /// 12 pontos fixos: mês corrente + 11 anteriores.
    /// Meses sem novos estabelecimentos retornam total = 0 (gerado via generate_series).
    /// Formato do mês: "YYYY-MM".
    /// </summary>
    public async Task<IReadOnlyList<CrescimentoMensalPontoDto>> ObterCrescimentoMensalAsync(
        CancellationToken ct = default)
    {
        const string sql = """
            WITH meses AS (
                SELECT TO_CHAR(m, 'YYYY-MM') AS Mes
                FROM   generate_series(
                           DATE_TRUNC('month', NOW()) - INTERVAL '11 months',
                           DATE_TRUNC('month', NOW()),
                           INTERVAL '1 month'
                       ) AS m
            ),
            contagens AS (
                SELECT TO_CHAR(DATE_TRUNC('month', criado_em), 'YYYY-MM') AS Mes,
                       COUNT(*)::int AS Total
                FROM   public.estabelecimentos
                WHERE  criado_em >= DATE_TRUNC('month', NOW()) - INTERVAL '11 months'
                GROUP  BY 1
            )
            SELECT m.Mes, COALESCE(c.Total, 0) AS Total
            FROM   meses m
            LEFT JOIN contagens c ON c.Mes = m.Mes
            ORDER  BY m.Mes
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.QueryAsync<CrescimentoMensalPontoDto>(
            new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    /// <summary>
    /// Alertas acionáveis:
    /// - Trials expirando em 7 dias (LIMIT 10).
    /// - Estabelecimentos ativos sem assinatura vigente (LIMIT 10 + total).
    /// </summary>
    public async Task<AlertasDashboardDto> ObterAlertasAsync(CancellationToken ct = default)
    {
        const string sqlTrials = """
            SELECT
                e.id                    AS EstabelecimentoId,
                e.nome_fantasia         AS NomeFantasia,
                u.nome_completo         AS DonoNome,
                ia.fim_em               AS FimEm,
                EXTRACT(DAY FROM ia.fim_em - NOW())::int AS DiasRestantes
            FROM   public.imedto_assinaturas ia
            INNER JOIN public.estabelecimentos e ON e.id = ia.estabelecimento_id
            INNER JOIN public.usuarios u ON u.id = e.dono_usuario_id
            WHERE  ia.gratuita = TRUE
              AND  ia.fim_em BETWEEN NOW() AND NOW() + INTERVAL '7 days'
            ORDER  BY ia.fim_em
            LIMIT  10
            """;

        const string sqlSemAssinaturaItens = """
            SELECT
                e.id                    AS EstabelecimentoId,
                e.nome_fantasia         AS NomeFantasia,
                u.nome_completo         AS DonoNome,
                e.criado_em             AS CriadoEm
            FROM   public.estabelecimentos e
            INNER JOIN public.usuarios u ON u.id = e.dono_usuario_id
            WHERE  e.status = 'Ativo'
              AND  NOT EXISTS (
                    SELECT 1 FROM public.imedto_assinaturas ia
                    WHERE  ia.estabelecimento_id = e.id AND ia.fim_em IS NULL
               )
            ORDER  BY e.nome_fantasia
            LIMIT  10
            """;

        const string sqlSemAssinaturaTotal = """
            SELECT COUNT(*)::int
            FROM   public.estabelecimentos e
            WHERE  e.status = 'Ativo'
              AND  NOT EXISTS (
                    SELECT 1 FROM public.imedto_assinaturas ia
                    WHERE  ia.estabelecimento_id = e.id AND ia.fim_em IS NULL
               )
            """;

        await using var conn = new NpgsqlConnection(_connStr);

        var trials = await conn.QueryAsync<TrialExpirandoDto>(
            new CommandDefinition(sqlTrials, cancellationToken: ct));

        var semAssinatura = await conn.QueryAsync<SemAssinaturaDto>(
            new CommandDefinition(sqlSemAssinaturaItens, cancellationToken: ct));

        var semAssinaturaTotal = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlSemAssinaturaTotal, cancellationToken: ct));

        return new AlertasDashboardDto
        {
            TrialsExpirando = trials.ToList(),
            SemAssinatura = semAssinatura.ToList(),
            SemAssinaturaTotal = semAssinaturaTotal,
        };
    }

    /// <summary>
    /// Feed paginado de audit log com filtros opcionais.
    /// Período: "hoje" | "7d" | "30d" | "90d" | "todos". Default "7d".
    /// Usa índices existentes: (acao, criado_em), (admin_id, criado_em), (criado_em), (tenant_afetado_id, criado_em).
    /// </summary>
    public async Task<AuditLogPaginadoDto> ListarAuditLogAsync(
        string? acao,
        Guid? adminId,
        string periodo,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;
        var dataInicio = ResolverDataInicioPeriodo(periodo);

        const string sqlItens = """
            SELECT
                al.id                   AS Id,
                al.criado_em            AS CriadoEm,
                al.admin_id             AS AdminId,
                a.nome                  AS AdminNome,
                a.email                 AS AdminEmail,
                COALESCE(a.ativo, FALSE) AS AdminAtivo,
                al.acao                 AS Acao,
                al.recurso_tipo         AS RecursoTipo,
                al.recurso_id           AS RecursoId,
                al.tenant_afetado_id    AS TenantAfetadoId,
                e.nome_fantasia         AS TenantNomeFantasia,
                al.motivo               AS Motivo
            FROM   public.imedto_admin_audit_log al
            LEFT JOIN public.imedto_admins a ON a.id = al.admin_id
            LEFT JOIN public.estabelecimentos e ON e.id = al.tenant_afetado_id
            WHERE  (@Acao IS NULL OR al.acao = @Acao)
              AND  (@AdminId IS NULL OR al.admin_id = @AdminId)
              AND  (@DataInicio IS NULL OR al.criado_em >= @DataInicio)
            ORDER  BY al.criado_em DESC
            LIMIT  @Tamanho OFFSET @Offset
            """;

        const string sqlCount = """
            SELECT COUNT(*)::int
            FROM   public.imedto_admin_audit_log al
            WHERE  (@Acao IS NULL OR al.acao = @Acao)
              AND  (@AdminId IS NULL OR al.admin_id = @AdminId)
              AND  (@DataInicio IS NULL OR al.criado_em >= @DataInicio)
            """;

        var p = new
        {
            Acao = string.IsNullOrWhiteSpace(acao) ? (string?)null : acao.Trim(),
            AdminId = adminId,
            DataInicio = dataInicio,
            Tamanho = tamanhoPagina,
            Offset = offset,
        };

        await using var conn = new NpgsqlConnection(_connStr);

        var itens = await conn.QueryAsync<AuditLogItemDto>(
            new CommandDefinition(sqlItens, p, cancellationToken: ct));
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, p, cancellationToken: ct));

        return new AuditLogPaginadoDto
        {
            Itens = itens.ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }

    private static DateTimeOffset? ResolverDataInicioPeriodo(string periodo) => periodo switch
    {
        "hoje"  => new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero),
        "7d"    => DateTimeOffset.UtcNow.AddDays(-7),
        "30d"   => DateTimeOffset.UtcNow.AddDays(-30),
        "90d"   => DateTimeOffset.UtcNow.AddDays(-90),
        "todos" => (DateTimeOffset?)null,
        _       => DateTimeOffset.UtcNow.AddDays(-7), // default seguro
    };
}
