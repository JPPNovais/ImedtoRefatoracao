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
        const string sqlPront = """
            SELECT  p.id                         AS Id,
                    p.paciente_id                AS PacienteId,
                    p.estabelecimento_id         AS EstabelecimentoId,
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
                    e.autor_usuario_id               AS AutorUsuarioId,
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
}
