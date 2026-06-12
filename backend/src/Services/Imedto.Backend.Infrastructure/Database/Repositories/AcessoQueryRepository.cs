using Dapper;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Contrato de leitura do relatório de acessos LGPD.
/// Interface aqui (infra) — mesmo padrão de IDocumentoQueryRepository.
/// </summary>
public interface IAcessoQueryRepository
{
    Task<PaginaAcessosDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina);
}

/// <summary>
/// Read-side agregado de acessos LGPD (Dapper).
/// UNION ALL sobre paciente_acesso_log e prontuario_acesso_log,
/// ordered por ocorrido_em desc, paginado server-side.
///
/// Rótulo leigo montado aqui (CASE WHEN no SELECT) — fonte única,
/// reusado pela lista e pelo PDF (CA5/R6). Multi-tenant: ambas as
/// subconsultas filtram estabelecimento_id (R2/CA8).
///
/// paciente_acesso_log usa índice (paciente_id, ocorrido_em).
/// prontuario_acesso_log usa índice (prontuario_id, ocorrido_em)
/// + JOIN prontuarios.paciente_id (1:1).
/// </summary>
public class AcessoQueryRepository : IAcessoQueryRepository
{
    private readonly string _connStr;

    public AcessoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaAcessosDto> ListarDoPaciente(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina)
    {
        // ── Subconsulta de acessos ao cadastro do paciente ────────────────────
        // Índice (paciente_id, ocorrido_em) cobre diretamente.
        const string sqlPaciente = """
            SELECT
                COALESCE(u.nome_completo, 'Usuário removido')  AS Quem,
                pal.ocorrido_em                                 AS Quando,
                'Cadastro/dados do paciente'                    AS Recurso,
                CASE pal.tipo_acesso
                    WHEN 'Leitura'       THEN 'Visualizou os dados'
                    WHEN 'Edicao'        THEN 'Atualizou os dados'
                    WHEN 'Exclusao'      THEN 'Removeu o cadastro'
                    WHEN 'Export'        THEN 'Exportou os dados (portabilidade)'
                    WHEN 'Anonimizacao'  THEN 'Anonimizou os dados'
                    ELSE pal.tipo_acesso
                END                                             AS Acao
            FROM public.paciente_acesso_log pal
            LEFT JOIN public.usuarios u ON u.id = pal.usuario_id
            WHERE pal.paciente_id        = @PacienteId
              AND pal.estabelecimento_id = @EstabelecimentoId
            """;

        // ── Subconsulta de acessos ao prontuário ──────────────────────────────
        // Mapeia prontuario → paciente via prontuarios.paciente_id (1:1).
        // Índice (prontuario_id, ocorrido_em) cobre o lado do log;
        // prontuarios.paciente_id deve ter índice para o JOIN (ver hand-off).
        const string sqlProntuario = """
            SELECT
                COALESCE(u.nome_completo, 'Usuário removido')  AS Quem,
                pal.ocorrido_em                                 AS Quando,
                'Prontuário'                                    AS Recurso,
                CASE pal.tipo_acesso
                    WHEN 'Leitura'    THEN 'Consultou o prontuário'
                    WHEN 'Escrita'    THEN 'Registrou no prontuário'
                    WHEN 'Exportacao' THEN 'Exportou o prontuário (PDF)'
                    ELSE pal.tipo_acesso
                END                                             AS Acao
            FROM public.prontuario_acesso_log pal
            INNER JOIN public.prontuarios pr
                   ON pr.id = pal.prontuario_id
                  AND pr.paciente_id = @PacienteId
            LEFT JOIN public.usuarios u ON u.id = pal.usuario_id
            WHERE pal.estabelecimento_id = @EstabelecimentoId
            """;

        var unionSql = $"{sqlPaciente}\nUNION ALL\n{sqlProntuario}";

        var sqlTotal = $"""
            SELECT COUNT(*) FROM (
            {unionSql}
            ) AS acessos
            """;

        var sqlItens = $"""
            SELECT Quem, Quando, Recurso, Acao FROM (
            {unionSql}
            ) AS acessos
            ORDER BY acessos.quando DESC
            LIMIT @Tamanho OFFSET @Offset
            """;

        var parametros = new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId,
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        };

        await using var conn = new NpgsqlConnection(_connStr);

        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, parametros);
        var itens = await conn.QueryAsync<AcessoPacienteResumoDto>(sqlItens, parametros);

        return new PaginaAcessosDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }
}
