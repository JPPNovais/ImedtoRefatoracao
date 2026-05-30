using Dapper;
using Npgsql;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;

namespace Imedto.Backend.Infrastructure.Admin;

public interface IAdminEstabelecimentosQueryRepository
{
    Task<(IEnumerable<EstabelecimentoAdminListaItemDto> Itens, int Total)> ListarAsync(
        string? busca, string? status, int pagina, int tamanhoPagina, CancellationToken ct = default);

    Task<EstabelecimentoAdminDetalheDto?> ObterDetalheAsync(long id, CancellationToken ct = default);

    Task<(string? Cpf, string? NomeFantasia)> ObterCpfENomeFantasiaAsync(
        long estabelecimentoId, CancellationToken ct = default);
}

/// <summary>
/// Read repository (Dapper) para queries da área admin sobre estabelecimentos.
/// Não filtra por estabelecimento_id: admin global lê todos os tenants.
/// LGPD: CPF mascarado em C# antes de expor no DTO. Zero campo de paciente.
///
/// Plano vigente = imedto_assinaturas.fim_em IS NULL para o estabelecimento.
/// </summary>
public class AdminEstabelecimentosQueryRepository : IAdminEstabelecimentosQueryRepository
{
    private readonly string _connStr;

    public AdminEstabelecimentosQueryRepository(AppReadConnectionString connStr)
    {
        _connStr = connStr.Value;
    }

    /// <summary>
    /// "12345678900" → "123.***.***-00". Aceita CPF com ou sem formatação.
    /// Retorna placeholder se CPF nulo/vazio.
    /// </summary>
    internal static string MascaraCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return "***.***.***-**";
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return "***.***.***-**";
        return $"{digits[..3]}.***.***-{digits[9..]}";
    }

    /// <summary>
    /// Lista paginada. Busca usa índice GIN pg_trgm em nome_fantasia (CA24–CA26).
    /// Filtro de status via coluna direta.
    /// </summary>
    public async Task<(IEnumerable<EstabelecimentoAdminListaItemDto> Itens, int Total)> ListarAsync(
        string? busca,
        string? status,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var offset = (pagina - 1) * tamanhoPagina;

        const string sqlItens = """
            SELECT  e.id                                                        AS Id,
                    e.nome_fantasia                                              AS NomeFantasia,
                    e.razao_social                                               AS RazaoSocial,
                    e.cnpj                                                       AS Cnpj,
                    e.status                                                     AS Status,
                    u.nome_completo                                              AS DonoNome,
                    u.email                                                      AS DonoEmail,
                    u.cpf                                                        AS DonoCpfBruto,
                    COALESCE(ip.nome, 'Sem plano')                               AS PlanoNome,
                    e.criado_em                                                  AS CriadoEm,
                    COALESCE(prof_count.total, 0)                                AS TotalProfissionaisAtivos,
                    COALESCE(pac_count.total, 0)                                 AS TotalPacientes,
                    COALESCE(ag_count.total, 0)                                  AS AgendamentosNoMes
            FROM    public.estabelecimentos e
            INNER JOIN public.usuarios u
                    ON u.id = e.dono_usuario_id
            LEFT JOIN public.imedto_assinaturas ia
                    ON ia.estabelecimento_id = e.id AND ia.fim_em IS NULL
            LEFT JOIN public.imedto_planos ip
                    ON ip.id = ia.plano_id
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.vinculo_profissional_estabelecimento v
                WHERE  v.estabelecimento_id = e.id AND v.status = 'Ativo'
            ) prof_count ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.pacientes pac
                WHERE  pac.estabelecimento_id = e.id AND pac.deletado_em IS NULL
            ) pac_count ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.agendamentos ag
                WHERE  ag.estabelecimento_id = e.id
                  AND  ag.inicio_previsto >= DATE_TRUNC('month', NOW())
            ) ag_count ON TRUE
            WHERE   (@Busca IS NULL OR e.nome_fantasia ILIKE '%' || @Busca || '%')
              AND   (@Status IS NULL OR e.status = @Status)
            ORDER BY e.nome_fantasia
            LIMIT   @Tamanho OFFSET @Offset
            """;

        const string sqlCount = """
            SELECT COUNT(*)::int
            FROM   public.estabelecimentos e
            WHERE  (@Busca IS NULL OR e.nome_fantasia ILIKE '%' || @Busca || '%')
              AND  (@Status IS NULL OR e.status = @Status)
            """;

        var p = new
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? (string?)null : busca.Trim(),
            Status = status,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        var rows = await conn.QueryAsync<EstabelecimentoAdminListaItemRaw>(
            new CommandDefinition(sqlItens, p, cancellationToken: ct));
        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlCount, p, cancellationToken: ct));

        var itens = rows.Select(r => new EstabelecimentoAdminListaItemDto
        {
            Id = r.Id,
            NomeFantasia = r.NomeFantasia,
            RazaoSocial = r.RazaoSocial,
            Cnpj = r.Cnpj,
            Status = r.Status,
            DonoNome = r.DonoNome,
            DonoEmail = r.DonoEmail,
            DonoCpfMascarado = MascaraCpf(r.DonoCpfBruto),
            PlanoNome = r.PlanoNome,
            CriadoEm = r.CriadoEm,
            TotalProfissionaisAtivos = r.TotalProfissionaisAtivos,
            TotalPacientes = r.TotalPacientes,
            AgendamentosNoMes = r.AgendamentosNoMes,
        });

        return (itens, total);
    }

    /// <summary>
    /// Detalhe completo. CPF mascarado. Zero campo de paciente.
    /// Retorna null se não encontrado.
    /// </summary>
    public async Task<EstabelecimentoAdminDetalheDto?> ObterDetalheAsync(long id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT  e.id                                                        AS Id,
                    e.nome_fantasia                                              AS NomeFantasia,
                    e.razao_social                                               AS RazaoSocial,
                    e.cnpj                                                       AS Cnpj,
                    e.status                                                     AS Status,
                    e.telefone                                                   AS Telefone,
                    e.cidade                                                     AS Cidade,
                    e.estado                                                     AS Estado,
                    e.criado_em                                                  AS CriadoEm,
                    u.id                                                         AS DonoUsuarioId,
                    u.nome_completo                                              AS DonoNome,
                    u.email                                                      AS DonoEmail,
                    u.cpf                                                        AS DonoCpfBruto,
                    COALESCE(ip.nome, 'Sem plano')                               AS PlanoNome,
                    COALESCE(ia.gratuita, FALSE)                                 AS AssinaturaGratuita,
                    ia.fim_em                                                    AS AssinaturaDataFim,
                    COALESCE(prof_count.total, 0)                                AS TotalProfissionaisAtivos,
                    COALESCE(pac_count.total, 0)                                 AS TotalPacientes,
                    COALESCE(ag_count.total, 0)                                  AS AgendamentosNoMes,
                    COALESCE(pront_count.total, 0)                               AS TotalProntuarios
            FROM    public.estabelecimentos e
            INNER JOIN public.usuarios u
                    ON u.id = e.dono_usuario_id
            LEFT JOIN public.imedto_assinaturas ia
                    ON ia.estabelecimento_id = e.id AND ia.fim_em IS NULL
            LEFT JOIN public.imedto_planos ip
                    ON ip.id = ia.plano_id
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.vinculo_profissional_estabelecimento v
                WHERE  v.estabelecimento_id = e.id AND v.status = 'Ativo'
            ) prof_count ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.pacientes pac
                WHERE  pac.estabelecimento_id = e.id AND pac.deletado_em IS NULL
            ) pac_count ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.agendamentos ag
                WHERE  ag.estabelecimento_id = e.id
                  AND  ag.inicio_previsto >= DATE_TRUNC('month', NOW())
            ) ag_count ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS total
                FROM   public.prontuarios pr
                WHERE  pr.estabelecimento_id = e.id AND pr.deletado_em IS NULL
            ) pront_count ON TRUE
            WHERE   e.id = @Id
            """;

        var row = await conn.QueryFirstOrDefaultAsync<EstabelecimentoAdminDetalheRaw>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        if (row is null) return null;

        return new EstabelecimentoAdminDetalheDto
        {
            Id = row.Id,
            NomeFantasia = row.NomeFantasia,
            RazaoSocial = row.RazaoSocial,
            Cnpj = row.Cnpj,
            Status = row.Status,
            Telefone = row.Telefone,
            Cidade = row.Cidade,
            Estado = row.Estado,
            CriadoEm = row.CriadoEm,
            DonoUsuarioId = row.DonoUsuarioId,
            DonoNome = row.DonoNome,
            DonoEmail = row.DonoEmail,
            DonoCpfMascarado = MascaraCpf(row.DonoCpfBruto),
            PlanoNome = row.PlanoNome,
            AssinaturaGratuita = row.AssinaturaGratuita,
            AssinaturaDataFim = row.AssinaturaDataFim,
            TotalProfissionaisAtivos = row.TotalProfissionaisAtivos,
            TotalPacientes = row.TotalPacientes,
            AgendamentosNoMes = row.AgendamentosNoMes,
            TotalProntuarios = row.TotalProntuarios,
        };
    }

    /// <summary>
    /// Retorna CPF completo (bruto, não mascarado) e nome_fantasia.
    /// CPF bruto: usado por RevelarCpfDonoQueryHandler (formata CPF antes de retornar ao client).
    /// NomeFantasia: usado por ResetTenantCommandHandler para validar confirmação dupla.
    /// </summary>
    public async Task<(string? Cpf, string? NomeFantasia)> ObterCpfENomeFantasiaAsync(
        long estabelecimentoId,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT u.cpf, e.nome_fantasia
            FROM   public.estabelecimentos e
            INNER JOIN public.usuarios u ON u.id = e.dono_usuario_id
            WHERE  e.id = @Id
            """;

        return await conn.QueryFirstOrDefaultAsync<(string? Cpf, string? NomeFantasia)>(
            new CommandDefinition(sql, new { Id = estabelecimentoId }, cancellationToken: ct));
    }

    // ── Tipos intermediários com DonoCpfBruto isolado (nunca chega ao DTO diretamente) ─────────

    private class EstabelecimentoAdminListaItemRaw
    {
        public long Id { get; set; }
        public string NomeFantasia { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DonoNome { get; set; } = string.Empty;
        public string DonoEmail { get; set; } = string.Empty;
        public string? DonoCpfBruto { get; set; }
        public string PlanoNome { get; set; } = string.Empty;
        public DateTimeOffset CriadoEm { get; set; }
        public int TotalProfissionaisAtivos { get; set; }
        public int TotalPacientes { get; set; }
        public int AgendamentosNoMes { get; set; }
    }

    private class EstabelecimentoAdminDetalheRaw
    {
        public long Id { get; set; }
        public string NomeFantasia { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string Cnpj { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public DateTimeOffset CriadoEm { get; set; }
        public Guid DonoUsuarioId { get; set; }
        public string DonoNome { get; set; } = string.Empty;
        public string DonoEmail { get; set; } = string.Empty;
        public string? DonoCpfBruto { get; set; }
        public string PlanoNome { get; set; } = string.Empty;
        public bool AssinaturaGratuita { get; set; }
        public DateTimeOffset? AssinaturaDataFim { get; set; }
        public int TotalProfissionaisAtivos { get; set; }
        public int TotalPacientes { get; set; }
        public int AgendamentosNoMes { get; set; }
        public int TotalProntuarios { get; set; }
    }
}
