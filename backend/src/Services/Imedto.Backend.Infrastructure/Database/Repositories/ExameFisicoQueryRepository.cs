using Dapper;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side do exame físico (Dapper). Sempre filtra por <c>estabelecimento_id</c> (multi-tenant)
/// e <c>deletado_em IS NULL</c>. Versões "leves" (resumo) não trazem regiões.
/// </summary>
public class ExameFisicoQueryRepository
{
    private readonly string _connectionString;

    public ExameFisicoQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    /// <summary>
    /// Retorna o exame + ProntuarioId para audit log. ProntuarioId NAO faz parte do
    /// DTO publico (LGPD: redundante para o front), mas o handler precisa para gravar
    /// no ProntuarioAcessoLog.
    /// </summary>
    public async Task<(ExameFisicoDto Exame, long ProntuarioId)?> ObterCompleto(long id, long estabelecimentoId)
    {
        // SELECT minimizado (LGPD): sem paciente_id e realizado_por_usuario_id.
        // ProntuarioId vem como tupla — usado so internamente para audit log.
        const string sqlExame = """
            SELECT  e.id                           AS Id,
                    e.evolucao_id                  AS EvolucaoId,
                    e.realizado_em                 AS RealizadoEm,
                    u.nome_completo                AS RealizadoPorNome,
                    e.dados_gerais_json            AS DadosGeraisJson,
                    e.observacoes_gerais           AS ObservacoesGerais,
                    e.criado_em                    AS CriadoEm,
                    e.atualizado_em                AS AtualizadoEm,
                    e.prontuario_id                AS ProntuarioId
            FROM    public.exame_fisico e
            LEFT JOIN public.usuarios u ON u.id = e.realizado_por_usuario_id
            WHERE   e.id = @Id
              AND   e.estabelecimento_id = @EstabelecimentoId
              AND   e.deletado_em IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync<(long Id, long EvolucaoId, DateTime RealizadoEm, string? RealizadoPorNome, string? DadosGeraisJson, string? ObservacoesGerais, DateTime CriadoEm, DateTime? AtualizadoEm, long ProntuarioId)?>(sqlExame, new
        {
            Id = id,
            EstabelecimentoId = estabelecimentoId
        });

        if (row is null) return null;

        var exame = new ExameFisicoDto
        {
            Id = row.Value.Id,
            EvolucaoId = row.Value.EvolucaoId,
            RealizadoEm = row.Value.RealizadoEm,
            RealizadoPorNome = row.Value.RealizadoPorNome,
            DadosGeraisJson = row.Value.DadosGeraisJson,
            ObservacoesGerais = row.Value.ObservacoesGerais,
            CriadoEm = row.Value.CriadoEm,
            AtualizadoEm = row.Value.AtualizadoEm,
            Regioes = await CarregarRegioes(conn, row.Value.Id)
        };
        return (exame, row.Value.ProntuarioId);
    }

    public async Task<(ExameFisicoDto Exame, long ProntuarioId)?> ObterPorEvolucao(long evolucaoId, long estabelecimentoId)
    {
        const string sqlExame = """
            SELECT  e.id                           AS Id,
                    e.evolucao_id                  AS EvolucaoId,
                    e.realizado_em                 AS RealizadoEm,
                    u.nome_completo                AS RealizadoPorNome,
                    e.dados_gerais_json            AS DadosGeraisJson,
                    e.observacoes_gerais           AS ObservacoesGerais,
                    e.criado_em                    AS CriadoEm,
                    e.atualizado_em                AS AtualizadoEm,
                    e.prontuario_id                AS ProntuarioId
            FROM    public.exame_fisico e
            LEFT JOIN public.usuarios u ON u.id = e.realizado_por_usuario_id
            WHERE   e.evolucao_id = @EvolucaoId
              AND   e.estabelecimento_id = @EstabelecimentoId
              AND   e.deletado_em IS NULL
            ORDER BY e.criado_em DESC
            LIMIT   1
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QuerySingleOrDefaultAsync<(long Id, long EvolucaoId, DateTime RealizadoEm, string? RealizadoPorNome, string? DadosGeraisJson, string? ObservacoesGerais, DateTime CriadoEm, DateTime? AtualizadoEm, long ProntuarioId)?>(sqlExame, new
        {
            EvolucaoId = evolucaoId,
            EstabelecimentoId = estabelecimentoId
        });

        if (row is null) return null;

        var exame = new ExameFisicoDto
        {
            Id = row.Value.Id,
            EvolucaoId = row.Value.EvolucaoId,
            RealizadoEm = row.Value.RealizadoEm,
            RealizadoPorNome = row.Value.RealizadoPorNome,
            DadosGeraisJson = row.Value.DadosGeraisJson,
            ObservacoesGerais = row.Value.ObservacoesGerais,
            CriadoEm = row.Value.CriadoEm,
            AtualizadoEm = row.Value.AtualizadoEm,
            Regioes = await CarregarRegioes(conn, row.Value.Id)
        };
        return (exame, row.Value.ProntuarioId);
    }

    public async Task<ListagemExamesResult> ListarDoPaciente(long pacienteId, long estabelecimentoId, int pagina, int tamanho)
    {
        const string sqlTotal = """
            SELECT COUNT(*)
            FROM   public.exame_fisico e
            WHERE  e.paciente_id = @PacienteId
              AND  e.estabelecimento_id = @EstabelecimentoId
              AND  e.deletado_em IS NULL
            """;

        // Subquery para severidade máxima — ordem clínica: Critico > Alterado > LeveAlteracao > Normal.
        const string sqlItens = """
            SELECT  e.id                           AS Id,
                    e.evolucao_id                  AS EvolucaoId,
                    e.realizado_em                 AS RealizadoEm,
                    e.prontuario_id                AS ProntuarioId,
                    u.nome_completo                AS RealizadoPorNome,
                    (SELECT COUNT(*) FROM public.exame_fisico_regioes r WHERE r.exame_fisico_id = e.id) AS TotalRegioes,
                    (e.dados_gerais_json IS NOT NULL) AS TemDadosGerais,
                    (
                        SELECT r.severidade
                        FROM   public.exame_fisico_regioes r
                        WHERE  r.exame_fisico_id = e.id AND r.severidade IS NOT NULL
                        ORDER BY CASE r.severidade
                                    WHEN 'Critico' THEN 4
                                    WHEN 'Alterado' THEN 3
                                    WHEN 'LeveAlteracao' THEN 2
                                    WHEN 'Normal' THEN 1
                                    ELSE 0
                                 END DESC
                        LIMIT 1
                    ) AS SeveridadeMaxima
            FROM    public.exame_fisico e
            LEFT JOIN public.usuarios u ON u.id = e.realizado_por_usuario_id
            WHERE   e.paciente_id = @PacienteId
              AND   e.estabelecimento_id = @EstabelecimentoId
              AND   e.deletado_em IS NULL
            ORDER BY e.realizado_em DESC
            LIMIT   @Tamanho OFFSET @Offset
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var total = await conn.ExecuteScalarAsync<int>(sqlTotal, new { PacienteId = pacienteId, EstabelecimentoId = estabelecimentoId });

        var rows = (await conn.QueryAsync<(long Id, long EvolucaoId, DateTime RealizadoEm, long ProntuarioId, string? RealizadoPorNome, int TotalRegioes, bool TemDadosGerais, string? SeveridadeMaxima)>(
            sqlItens,
            new
            {
                PacienteId = pacienteId,
                EstabelecimentoId = estabelecimentoId,
                Tamanho = tamanho,
                Offset = (pagina - 1) * tamanho
            })).ToList();

        var primeiroProntuarioId = rows.FirstOrDefault().ProntuarioId;

        var itens = rows.Select(r => new ExameFisicoResumoDto
        {
            Id = r.Id,
            EvolucaoId = r.EvolucaoId,
            RealizadoEm = r.RealizadoEm,
            RealizadoPorNome = r.RealizadoPorNome,
            TotalRegioes = r.TotalRegioes,
            TemDadosGerais = r.TemDadosGerais,
            SeveridadeMaxima = r.SeveridadeMaxima
        }).ToList();

        return new ListagemExamesResult(itens, total, primeiroProntuarioId);
    }

    public async Task<TimelineExamesResult> Timeline(long pacienteId, long estabelecimentoId, int ate)
    {
        const string sql = """
            SELECT  e.id                           AS Id,
                    e.evolucao_id                  AS EvolucaoId,
                    e.realizado_em                 AS RealizadoEm,
                    e.prontuario_id                AS ProntuarioId,
                    u.nome_completo                AS RealizadoPorNome,
                    (SELECT COUNT(*) FROM public.exame_fisico_regioes r WHERE r.exame_fisico_id = e.id) AS TotalRegioes,
                    (e.dados_gerais_json IS NOT NULL) AS TemDadosGerais,
                    (
                        SELECT r.severidade
                        FROM   public.exame_fisico_regioes r
                        WHERE  r.exame_fisico_id = e.id AND r.severidade IS NOT NULL
                        ORDER BY CASE r.severidade
                                    WHEN 'Critico' THEN 4
                                    WHEN 'Alterado' THEN 3
                                    WHEN 'LeveAlteracao' THEN 2
                                    WHEN 'Normal' THEN 1
                                    ELSE 0
                                 END DESC
                        LIMIT 1
                    ) AS SeveridadeMaxima
            FROM    public.exame_fisico e
            LEFT JOIN public.usuarios u ON u.id = e.realizado_por_usuario_id
            WHERE   e.paciente_id = @PacienteId
              AND   e.estabelecimento_id = @EstabelecimentoId
              AND   e.deletado_em IS NULL
            ORDER BY e.realizado_em DESC
            LIMIT   @Ate
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = (await conn.QueryAsync<(long Id, long EvolucaoId, DateTime RealizadoEm, long ProntuarioId, string? RealizadoPorNome, int TotalRegioes, bool TemDadosGerais, string? SeveridadeMaxima)>(
            sql,
            new { PacienteId = pacienteId, EstabelecimentoId = estabelecimentoId, Ate = ate })).ToList();

        var itens = rows.Select(r => new ExameFisicoResumoDto
        {
            Id = r.Id,
            EvolucaoId = r.EvolucaoId,
            RealizadoEm = r.RealizadoEm,
            RealizadoPorNome = r.RealizadoPorNome,
            TotalRegioes = r.TotalRegioes,
            TemDadosGerais = r.TemDadosGerais,
            SeveridadeMaxima = r.SeveridadeMaxima
        }).ToList();

        return new TimelineExamesResult(itens, rows.FirstOrDefault().ProntuarioId);
    }

    private static async Task<IEnumerable<RegiaoExameFisicoDto>> CarregarRegioes(NpgsqlConnection conn, long exameFisicoId)
    {
        const string sql = """
            SELECT  id                  AS Id,
                    regiao_codigo       AS RegiaoCodigo,
                    regiao_pai_codigo   AS RegiaoPaiCodigo,
                    lateralidade        AS Lateralidade,
                    achados             AS Achados,
                    severidade          AS Severidade,
                    ordem               AS Ordem
            FROM    public.exame_fisico_regioes
            WHERE   exame_fisico_id = @ExameFisicoId
            ORDER BY ordem, regiao_codigo
            """;

        return await conn.QueryAsync<RegiaoExameFisicoDto>(sql, new { ExameFisicoId = exameFisicoId });
    }

    public record ListagemExamesResult(IEnumerable<ExameFisicoResumoDto> Itens, int Total, long PrimeiroProntuarioId);
    public record TimelineExamesResult(IEnumerable<ExameFisicoResumoDto> Itens, long PrimeiroProntuarioId);
}
