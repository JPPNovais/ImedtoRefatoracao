using Dapper;
using Imedto.Backend.Contracts.Atestados.Queries.Results;
using Imedto.Backend.SharedKernel.Tenancy;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side de atestados (Dapper). Sempre filtra por <c>estabelecimento_id</c>
/// e <c>deletado_em IS NULL</c>.
/// </summary>
public interface IAtestadoQueryRepository
{
    Task<PaginaAtestadosDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel);
    Task<AtestadoDto?> ObterPorId(
        long atestadoId,
        long estabelecimentoId,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel);
    Task<IReadOnlyList<ModeloAtestadoDto>> ListarModelos(long estabelecimentoId);
}

public class AtestadoQueryRepository : IAtestadoQueryRepository
{
    private readonly string _connStr;

    public AtestadoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaAtestadosDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel)
    {
        // Gating: @Papel = 'Dono' bypassa; profissional vê só os próprios (R1 briefing 2026-06-27_001).
        const string sqlTotal = """
            SELECT COUNT(*)
            FROM   public.atestados a
            WHERE  a.paciente_id = @PacienteId
              AND  a.estabelecimento_id = @EstabelecimentoId
              AND  a.deletado_em IS NULL
              AND  (@Papel = 'Dono' OR a.profissional_usuario_id = @UsuarioId)
            """;

        const string sqlItens = """
            SELECT  a.id                  AS Id,
                    a.paciente_id         AS PacienteId,
                    a.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    a.tipo                AS Tipo,
                    a.dias_afastamento    AS DiasAfastamento,
                    a.cid10               AS Cid10,
                    a.conteudo            AS Conteudo,
                    a.criado_em           AS CriadoEm
            FROM    public.atestados a
            LEFT JOIN public.usuarios u ON u.id = a.profissional_usuario_id
            WHERE   a.paciente_id = @PacienteId
              AND   a.estabelecimento_id = @EstabelecimentoId
              AND   a.deletado_em IS NULL
              AND   (@Papel = 'Dono' OR a.profissional_usuario_id = @UsuarioId)
            ORDER BY a.criado_em DESC
            LIMIT   @Tamanho OFFSET @Offset
            """;

        var parametros = new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = solicitantePapel.ToString(),
        };

        await using var conn = new NpgsqlConnection(_connStr);
        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, parametros);

        var itens = await conn.QueryAsync<AtestadoDto>(sqlItens, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = solicitantePapel.ToString(),
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        });

        return new PaginaAtestadosDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }

    public async Task<AtestadoDto?> ObterPorId(
        long atestadoId,
        long estabelecimentoId,
        Guid solicitanteUsuarioId,
        TenantPapel solicitantePapel)
    {
        // Gating: retorna null se o atestado não é do solicitante (e não é Dono) — mensagem genérica no handler (R5).
        const string sql = """
            SELECT  a.id                  AS Id,
                    a.paciente_id         AS PacienteId,
                    a.profissional_usuario_id AS ProfissionalUsuarioId,
                    u.nome_completo       AS ProfissionalNome,
                    a.tipo                AS Tipo,
                    a.dias_afastamento    AS DiasAfastamento,
                    a.cid10               AS Cid10,
                    a.conteudo            AS Conteudo,
                    a.criado_em           AS CriadoEm
            FROM    public.atestados a
            LEFT JOIN public.usuarios u ON u.id = a.profissional_usuario_id
            WHERE   a.id = @Id
              AND   a.estabelecimento_id = @EstabelecimentoId
              AND   a.deletado_em IS NULL
              AND   (@Papel = 'Dono' OR a.profissional_usuario_id = @UsuarioId)
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<AtestadoDto>(sql, new
        {
            Id = atestadoId,
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = solicitanteUsuarioId,
            Papel = solicitantePapel.ToString(),
        });
    }

    public async Task<IReadOnlyList<ModeloAtestadoDto>> ListarModelos(long estabelecimentoId)
    {
        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    tipo            AS Tipo,
                    conteudo        AS Conteudo,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.modelos_atestado
            WHERE   estabelecimento_id = @EstabelecimentoId
            ORDER BY nome
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        var itens = await conn.QueryAsync<ModeloAtestadoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
        });
        return itens.ToList();
    }
}
