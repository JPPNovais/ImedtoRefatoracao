using Dapper;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.Domain.Common;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read repository (Dapper) para vínculos — consultas agregadas com JOINs.
/// </summary>
public class VinculoQueryRepository
{
    private readonly string _connectionString;
    private readonly IFotoStorageService _fotoStorage;

    public VinculoQueryRepository(AppReadConnectionString connection, IFotoStorageService fotoStorage)
    {
        _connectionString = connection.Value;
        _fotoStorage = fotoStorage;
    }

    /// <summary>
    /// Profissionais do estabelecimento (Dono UNION vínculos). Por default só
    /// retorna não-inativos — reflete <c>IVinculoRepository.PodeAtuarComoProfissional</c>
    /// para seletores de agenda/prontuário. A tela de gestão de equipe passa
    /// <paramref name="incluirInativos"/>=true pra também enxergar quem foi desativado
    /// (UX: ao Desativar, a linha continua visível em "Inativos" em vez de "sumir").
    /// </summary>
    public async Task<IEnumerable<ProfissionalVinculadoDto>> ListarProfissionaisDoEstabelecimento(long estabelecimentoId, bool incluirInativos = false)
    {
        var filtroStatus = incluirInativos ? string.Empty : "AND v.status <> 'Inativo'";
        var sql = $$"""
            SELECT  v.id                       AS VinculoId,
                    v.profissional_usuario_id  AS UsuarioId,
                    u.email                    AS Email,
                    COALESCE(u.nome_completo, v.nome_convidado) AS NomeCompleto,
                    v.status                   AS Status,
                    v.modelo_permissao_id      AS ModeloPermissaoId,
                    mp.nome                    AS ModeloPermissaoNome,
                    v.convidado_em             AS ConvidadoEm,
                    v.aceito_em                AS AceitoEm,
                    COALESCE(v.especialidade_convidada, p.especialidade) AS Especialidade,
                    p.conselho                 AS Conselho,
                    pr.nome                    AS Profissao,
                    v.profissao_convidada_id   AS ProfissaoConvidadaId,
                    p.foto_url                 AS FotoUrl
            FROM    public.vinculo_profissional_estabelecimento v
            JOIN    public.usuarios u ON u.id = v.profissional_usuario_id
            LEFT JOIN public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
            LEFT JOIN public.profissionais p ON p.usuario_id = v.profissional_usuario_id AND p.deletado_em IS NULL
            LEFT JOIN public.profissoes pr ON pr.id = v.profissao_convidada_id
            WHERE   v.estabelecimento_id = @EstabelecimentoId
              {{filtroStatus}}
              AND   v.profissional_usuario_id
                    <> (SELECT dono_usuario_id FROM public.estabelecimentos WHERE id = @EstabelecimentoId)

            UNION ALL

            SELECT  NULL::bigint               AS VinculoId,  -- sintético (Dono), front identifica por status='Dono'
                    e.dono_usuario_id          AS UsuarioId,
                    u.email                    AS Email,
                    u.nome_completo            AS NomeCompleto,
                    'Dono'                     AS Status,
                    NULL::bigint               AS ModeloPermissaoId,
                    'Dono do estabelecimento'  AS ModeloPermissaoNome,
                    e.criado_em                AS ConvidadoEm,
                    e.criado_em                AS AceitoEm,
                    p.especialidade            AS Especialidade,
                    p.conselho                 AS Conselho,
                    NULL::text                 AS Profissao,
                    NULL::bigint               AS ProfissaoConvidadaId,
                    p.foto_url                 AS FotoUrl
            FROM    public.estabelecimentos e
            JOIN    public.usuarios u ON u.id = e.dono_usuario_id
            LEFT JOIN public.profissionais p ON p.usuario_id = e.dono_usuario_id AND p.deletado_em IS NULL
            WHERE   e.id = @EstabelecimentoId

            ORDER BY NomeCompleto NULLS LAST, Email
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<ProfissionalVinculadoDto>(sql, new { EstabelecimentoId = estabelecimentoId });
        foreach (var r in rows)
            r.FotoUrl = _fotoStorage.GerarUrlLeitura(r.FotoUrl);
        return rows;
    }

    /// <summary>
    /// Versão minimizada (LGPD) da lista de profissionais do estabelecimento.
    /// Devolve apenas Dono + vínculos Ativos (sem Convidado, sem Inativo) e
    /// somente os campos públicos: nome, especialidade, conselho, status.
    /// SEM e-mail, sem modelo de permissão, sem datas, sem vínculoId.
    ///
    /// Usado pelos seletores (agenda/prontuário/orçamento) onde qualquer
    /// membro do tenant precisa enxergar "com quem agenda" sem ganhar acesso
    /// a PII da equipe (e-mail/permissões eram vazadas para Médico e Recepção).
    /// </summary>
    public async Task<IEnumerable<ProfissionalPublicoDto>> ListarProfissionaisPublicoDoEstabelecimento(long estabelecimentoId)
    {
        const string sql = """
            SELECT  v.profissional_usuario_id          AS UsuarioId,
                    COALESCE(u.nome_completo, v.nome_convidado) AS NomeCompleto,
                    COALESCE(v.especialidade_convidada, p.especialidade) AS Especialidade,
                    p.conselho                         AS Conselho,
                    'Ativo'                            AS Status,
                    p.foto_url                         AS FotoUrl
            FROM    public.vinculo_profissional_estabelecimento v
            JOIN    public.usuarios u ON u.id = v.profissional_usuario_id
            LEFT JOIN public.profissionais p ON p.usuario_id = v.profissional_usuario_id AND p.deletado_em IS NULL
            WHERE   v.estabelecimento_id = @EstabelecimentoId
              AND   v.status = 'Ativo'
              AND   v.profissional_usuario_id
                    <> (SELECT dono_usuario_id FROM public.estabelecimentos WHERE id = @EstabelecimentoId)

            UNION ALL

            SELECT  e.dono_usuario_id                  AS UsuarioId,
                    u.nome_completo                    AS NomeCompleto,
                    p.especialidade                    AS Especialidade,
                    p.conselho                         AS Conselho,
                    'Dono'                             AS Status,
                    p.foto_url                         AS FotoUrl
            FROM    public.estabelecimentos e
            JOIN    public.usuarios u ON u.id = e.dono_usuario_id
            LEFT JOIN public.profissionais p ON p.usuario_id = e.dono_usuario_id AND p.deletado_em IS NULL
            WHERE   e.id = @EstabelecimentoId

            ORDER BY NomeCompleto NULLS LAST
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<ProfissionalPublicoDto>(sql, new { EstabelecimentoId = estabelecimentoId });
        foreach (var r in rows)
            r.FotoUrl = _fotoStorage.GerarUrlLeitura(r.FotoUrl);
        return rows;
    }

    /// <summary>Verifica se o usuário tem vínculo ativo com o estabelecimento (não inclui donos).</summary>
    public async Task<bool> TemVinculoAtivo(Guid usuarioId, long estabelecimentoId)
    {
        const string sql = """
            SELECT 1
            FROM   public.vinculo_profissional_estabelecimento
            WHERE  profissional_usuario_id = @UsuarioId
              AND  estabelecimento_id = @EstabelecimentoId
              AND  status = 'Ativo'
            LIMIT 1
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { UsuarioId = usuarioId, EstabelecimentoId = estabelecimentoId });
        return result.HasValue;
    }

    /// <summary>
    /// Retorna o tipo_acesso do modelo de permissão do vínculo ativo.
    /// Null quando não há vínculo ativo.
    /// </summary>
    public async Task<string?> ObterTipoAcessoVinculoAtivo(Guid usuarioId, long estabelecimentoId)
    {
        const string sql = """
            SELECT mp.tipo_acesso
            FROM   public.vinculo_profissional_estabelecimento v
            JOIN   public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
            WHERE  v.profissional_usuario_id = @UsuarioId
              AND  v.estabelecimento_id = @EstabelecimentoId
              AND  v.status = 'Ativo'
            LIMIT 1
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<string?>(sql, new { UsuarioId = usuarioId, EstabelecimentoId = estabelecimentoId });
    }

    /// <summary>Convites pendentes para o usuário logado.</summary>
    public async Task<IEnumerable<ConviteDto>> ListarConvitesPendentes(Guid usuarioId)
    {
        const string sql = """
            -- SELECT minimizado (LGPD): c.email removido — front nao exibe.
            SELECT  v.id                       AS VinculoId,
                    v.estabelecimento_id       AS EstabelecimentoId,
                    e.nome_fantasia            AS NomeFantasiaEstabelecimento,
                    c.nome_completo            AS ConvidadoPorNome,
                    v.convidado_em             AS ConvidadoEm,
                    v.nome_convidado           AS NomeConvidado,
                    v.telefone_convidado       AS TelefoneConvidado,
                    v.especialidade_convidada  AS EspecialidadeConvidada,
                    v.profissao_convidada_id   AS ProfissaoConvidadaId,
                    pr.nome                    AS ProfissaoConvidadaNome,
                    v.modelo_permissao_id      AS ModeloPermissaoId
            FROM    public.vinculo_profissional_estabelecimento v
            JOIN    public.estabelecimentos e ON e.id = v.estabelecimento_id
            JOIN    public.usuarios c ON c.id = v.convidado_por_usuario_id
            LEFT JOIN public.profissoes pr ON pr.id = v.profissao_convidada_id
            WHERE   v.profissional_usuario_id = @UsuarioId
              AND   v.status = 'Convidado'
            ORDER BY v.convidado_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<ConviteDto>(sql, new { UsuarioId = usuarioId });
    }
}
