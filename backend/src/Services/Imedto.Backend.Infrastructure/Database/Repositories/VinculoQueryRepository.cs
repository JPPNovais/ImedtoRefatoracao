using Dapper;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read repository (Dapper) para vínculos — consultas agregadas com JOINs.
/// </summary>
public class VinculoQueryRepository
{
    private readonly string _connectionString;

    public VinculoQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    /// <summary>
    /// Atuantes do estabelecimento: vínculos não-inativos UNION com o dono.
    /// Reflete a regra unificada de <c>IVinculoRepository.PodeAtuarComoProfissional</c>.
    /// </summary>
    public async Task<IEnumerable<ProfissionalVinculadoDto>> ListarProfissionaisDoEstabelecimento(long estabelecimentoId)
    {
        const string sql = """
            SELECT  v.id                       AS VinculoId,
                    v.profissional_usuario_id  AS UsuarioId,
                    u.email                    AS Email,
                    COALESCE(u.nome_completo, v.nome_convidado) AS NomeCompleto,
                    v.status                   AS Status,
                    v.modelo_permissao_id      AS ModeloPermissaoId,
                    mp.nome                    AS ModeloPermissaoNome,
                    v.convidado_em             AS ConvidadoEm,
                    v.aceito_em                AS AceitoEm,
                    COALESCE(p.especialidade, v.especialidade_convidada) AS Especialidade,
                    p.conselho                 AS Conselho
            FROM    public.vinculo_profissional_estabelecimento v
            JOIN    public.usuarios u ON u.id = v.profissional_usuario_id
            LEFT JOIN public.modelo_permissao_estabelecimento mp ON mp.id = v.modelo_permissao_id
            LEFT JOIN public.profissionais p ON p.usuario_id = v.profissional_usuario_id
            WHERE   v.estabelecimento_id = @EstabelecimentoId
              AND   v.status <> 'Inativo'
              AND   v.profissional_usuario_id
                    <> (SELECT dono_usuario_id FROM public.estabelecimentos WHERE id = @EstabelecimentoId)

            UNION ALL

            SELECT  0::bigint                  AS VinculoId,
                    e.dono_usuario_id          AS UsuarioId,
                    u.email                    AS Email,
                    u.nome_completo            AS NomeCompleto,
                    'Dono'                     AS Status,
                    NULL::bigint               AS ModeloPermissaoId,
                    'Dono do estabelecimento'  AS ModeloPermissaoNome,
                    e.criado_em                AS ConvidadoEm,
                    e.criado_em                AS AceitoEm,
                    p.especialidade            AS Especialidade,
                    p.conselho                 AS Conselho
            FROM    public.estabelecimentos e
            JOIN    public.usuarios u ON u.id = e.dono_usuario_id
            LEFT JOIN public.profissionais p ON p.usuario_id = e.dono_usuario_id
            WHERE   e.id = @EstabelecimentoId

            ORDER BY NomeCompleto NULLS LAST, Email
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<ProfissionalVinculadoDto>(sql, new { EstabelecimentoId = estabelecimentoId });
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
            SELECT  v.id                       AS VinculoId,
                    v.estabelecimento_id       AS EstabelecimentoId,
                    e.nome_fantasia            AS NomeFantasiaEstabelecimento,
                    c.email                    AS ConvidadoPorEmail,
                    c.nome_completo            AS ConvidadoPorNome,
                    v.convidado_em             AS ConvidadoEm,
                    v.nome_convidado           AS NomeConvidado,
                    v.telefone_convidado       AS TelefoneConvidado,
                    v.especialidade_convidada  AS EspecialidadeConvidada,
                    v.modelo_permissao_id      AS ModeloPermissaoId
            FROM    public.vinculo_profissional_estabelecimento v
            JOIN    public.estabelecimentos e ON e.id = v.estabelecimento_id
            JOIN    public.usuarios c ON c.id = v.convidado_por_usuario_id
            WHERE   v.profissional_usuario_id = @UsuarioId
              AND   v.status = 'Convidado'
            ORDER BY v.convidado_em DESC
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<ConviteDto>(sql, new { UsuarioId = usuarioId });
    }
}
