using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side do prontuário (Dapper): retorna o prontuário + timeline de evoluções
/// em consultas agregadas. Todas escopadas por <c>estabelecimento_id</c>.
/// </summary>
public class ProntuarioQueryRepository
{
    private readonly string _connectionString;

    public ProntuarioQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<ProntuarioCompletoDto> ObterDoPaciente(long pacienteId, long estabelecimentoId, int tamanhoTimeline)
    {
        // SELECT minimizado (LGPD): sem paciente_id/estabelecimento_id (vem da rota)
        // e autor_usuario_id (Guid auth interno — front nao usa).
        const string sqlPront = """
            SELECT  p.id                         AS Id,
                    p.modelo_de_prontuario_id    AS ModeloDeProntuarioId,
                    m.nome                       AS ModeloNome,
                    m.estrutura                  AS ModeloEstrutura,
                    p.criado_em                  AS CriadoEm,
                    p.atualizado_em              AS AtualizadoEm
            FROM    public.prontuarios p
            JOIN    public.modelo_de_prontuario m ON m.id = p.modelo_de_prontuario_id
            WHERE   p.paciente_id = @PacienteId
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
            """;

        const string sqlEvo = """
            SELECT  e.id                             AS Id,
                    e.prontuario_id                  AS ProntuarioId,
                    u.nome_completo                  AS AutorNome,
                    mdp.nome                         AS ModeloNome,
                    e.conteudo                       AS Conteudo,
                    e.modelo_snapshot                AS ModeloSnapshot,
                    e.modelo_de_prontuario_id_origem AS ModeloDeProntuarioIdOrigem,
                    e.criada_em                      AS CriadaEm
            FROM    public.prontuario_evolucoes e
            LEFT JOIN public.usuarios u ON u.id = e.autor_usuario_id
            LEFT JOIN public.modelo_de_prontuario mdp ON mdp.id = e.modelo_de_prontuario_id_origem
            WHERE   e.prontuario_id = @ProntuarioId
              AND   e.deletado_em IS NULL
            ORDER BY e.criada_em DESC
            LIMIT   @Tamanho
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var prontuario = await conn.QuerySingleOrDefaultAsync<ProntuarioDto>(sqlPront, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });

        if (prontuario is null) return null;

        var evolucoes = await conn.QueryAsync<EvolucaoDto>(sqlEvo, new
        {
            ProntuarioId = prontuario.Id,
            Tamanho = Math.Clamp(tamanhoTimeline, 1, 500)
        });

        return new ProntuarioCompletoDto
        {
            Prontuario = prontuario,
            Evolucoes = evolucoes
        };
    }

    /// <summary>
    /// Listagem paginada das evoluções do prontuário do paciente. Retorna lista vazia
    /// + total 0 quando o paciente ainda não tem prontuário (o front exibe o CTA).
    /// </summary>
    public async Task<PaginaEvolucoesDto> ListarEvolucoesPaginadas(
        long pacienteId,
        long estabelecimentoId,
        int pagina,
        int tamanhoPagina)
    {
        const string sqlProntId = """
            SELECT  p.id
            FROM    public.prontuarios p
            WHERE   p.paciente_id = @PacienteId
              AND   p.estabelecimento_id = @EstabelecimentoId
              AND   p.deletado_em IS NULL
            """;

        const string sqlTotal = """
            SELECT COUNT(*)
            FROM   public.prontuario_evolucoes e
            WHERE  e.prontuario_id = @ProntuarioId
              AND  e.deletado_em IS NULL
            """;

        const string sqlItens = """
            SELECT  e.id                             AS Id,
                    e.prontuario_id                  AS ProntuarioId,
                    u.nome_completo                  AS AutorNome,
                    mdp.nome                         AS ModeloNome,
                    e.conteudo                       AS Conteudo,
                    e.modelo_snapshot                AS ModeloSnapshot,
                    e.modelo_de_prontuario_id_origem AS ModeloDeProntuarioIdOrigem,
                    e.criada_em                      AS CriadaEm
            FROM    public.prontuario_evolucoes e
            LEFT JOIN public.usuarios u ON u.id = e.autor_usuario_id
            LEFT JOIN public.modelo_de_prontuario mdp ON mdp.id = e.modelo_de_prontuario_id_origem
            WHERE   e.prontuario_id = @ProntuarioId
              AND   e.deletado_em IS NULL
            ORDER BY e.criada_em DESC
            LIMIT   @Tamanho OFFSET @Offset
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var prontuarioId = await conn.QuerySingleOrDefaultAsync<long?>(sqlProntId, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });

        if (prontuarioId is null)
        {
            return new PaginaEvolucoesDto
            {
                Itens = Array.Empty<EvolucaoDto>(),
                Total = 0,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina,
            };
        }

        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, new { ProntuarioId = prontuarioId.Value });

        var itens = await conn.QueryAsync<EvolucaoDto>(sqlItens, new
        {
            ProntuarioId = prontuarioId.Value,
            Tamanho = tamanhoPagina,
            Offset = (pagina - 1) * tamanhoPagina,
        });

        return new PaginaEvolucoesDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
        };
    }

    /// <summary>
    /// Conta evoluções não-deletadas do prontuário do paciente. 0 se ainda não tem prontuário.
    /// Filtro multi-tenant via join em <c>prontuarios.estabelecimento_id</c>.
    /// </summary>
    public async Task<int> ContarEvolucoes(long pacienteId, long estabelecimentoId)
    {
        const string sql = """
            SELECT COUNT(*)::int
            FROM   public.prontuario_evolucoes pe
            JOIN   public.prontuarios p ON p.id = pe.prontuario_id
            WHERE  p.paciente_id = @PacienteId
              AND  p.estabelecimento_id = @EstabelecimentoId
              AND  p.deletado_em IS NULL
              AND  pe.deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
    }
}
