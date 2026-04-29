using Dapper;
using Imedto.Backend.Contracts.Cirurgias;
using Imedto.Backend.Contracts.Cirurgias.Queries.Results;
using Npgsql;
using System.Text.Json;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Leitura via Dapper (escrita é feita pelo EF). Sempre filtra <c>deletado_em IS NULL</c>
/// e exige <c>estabelecimento_id</c> — defesa multi-tenant em camada de leitura.
/// </summary>
public class ProcedimentoCirurgicoQueryRepository
{
    private readonly string _connStr;

    public ProcedimentoCirurgicoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<ProcedimentoCirurgicoDto?> ObterCompleto(long id, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlProc = """
            SELECT
                p.id                    AS Id,
                p.estabelecimento_id    AS EstabelecimentoId,
                p.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                p.prontuario_id         AS ProntuarioId,
                p.agendamento_id        AS AgendamentoId,
                p.data_agendada         AS DataAgendada,
                p.data_realizada        AS DataRealizada,
                p.status                AS Status,
                p.cirurgia_principal    AS CirurgiaPrincipal,
                p.cirurgia_codigo       AS CirurgiaCodigo,
                p.descricao_cirurgica       AS DescricaoCirurgica,
                p.ficha_anestesica::text    AS FichaAnestesicaRaw,
                p.evolucao_pos_op           AS EvolucaoPosOp,
                p.observacoes           AS Observacoes,
                p.cancelado_em          AS CanceladoEm,
                p.motivo_cancelamento   AS MotivoCancelamento,
                p.criado_em             AS CriadoEm,
                p.atualizado_em         AS AtualizadoEm
            FROM procedimentos_cirurgicos p
            JOIN pacientes pac ON pac.id = p.paciente_id
            WHERE p.id = @Id
              AND p.estabelecimento_id = @EstabelecimentoId
              AND p.deletado_em IS NULL
            """;

        const string sqlEquipe = """
            SELECT
                e.id                       AS Id,
                e.profissional_usuario_id  AS ProfissionalUsuarioId,
                COALESCE(u.nome_completo, u.email) AS ProfissionalNome,
                e.papel                    AS Papel,
                e.ordem                    AS Ordem
            FROM equipe_cirurgica e
            LEFT JOIN usuarios u ON u.id = e.profissional_usuario_id
            WHERE e.procedimento_id = @Id
            ORDER BY e.ordem, e.id
            """;

        var raw = await conn.QuerySingleOrDefaultAsync<ProcedimentoCirurgicoDtoRaw>(
            sqlProc, new { Id = id, EstabelecimentoId = estabelecimentoId });
        if (raw is null) return null;

        var dto = raw.ToDto();
        var equipe = await conn.QueryAsync<MembroEquipeCirurgicaDto>(sqlEquipe, new { Id = id });
        dto.Equipe = equipe.ToList();
        return dto;
    }

    public async Task<IEnumerable<ProcedimentoCirurgicoResumoDto>> ListarDoPaciente(
        long pacienteId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                p.id                    AS Id,
                p.estabelecimento_id    AS EstabelecimentoId,
                p.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                p.prontuario_id         AS ProntuarioId,
                p.agendamento_id        AS AgendamentoId,
                p.data_agendada         AS DataAgendada,
                p.data_realizada        AS DataRealizada,
                p.status                AS Status,
                p.cirurgia_principal    AS CirurgiaPrincipal,
                p.cirurgia_codigo       AS CirurgiaCodigo,
                p.criado_em             AS CriadoEm
            FROM procedimentos_cirurgicos p
            JOIN pacientes pac ON pac.id = p.paciente_id
            WHERE p.paciente_id = @PacienteId
              AND p.estabelecimento_id = @EstabelecimentoId
              AND p.deletado_em IS NULL
            ORDER BY COALESCE(p.data_realizada, p.data_agendada, p.criado_em) DESC
            """;

        return await conn.QueryAsync<ProcedimentoCirurgicoResumoDto>(sql, new
        {
            PacienteId = pacienteId,
            EstabelecimentoId = estabelecimentoId
        });
    }

    public async Task<IEnumerable<ProcedimentoCirurgicoResumoDto>> ListarPlanejados(
        long estabelecimentoId, DateTime dataInicio, DateTime dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                p.id                    AS Id,
                p.estabelecimento_id    AS EstabelecimentoId,
                p.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                p.prontuario_id         AS ProntuarioId,
                p.agendamento_id        AS AgendamentoId,
                p.data_agendada         AS DataAgendada,
                p.data_realizada        AS DataRealizada,
                p.status                AS Status,
                p.cirurgia_principal    AS CirurgiaPrincipal,
                p.cirurgia_codigo       AS CirurgiaCodigo,
                p.criado_em             AS CriadoEm
            FROM procedimentos_cirurgicos p
            JOIN pacientes pac ON pac.id = p.paciente_id
            WHERE p.estabelecimento_id = @EstabelecimentoId
              AND p.deletado_em IS NULL
              AND p.status IN ('Planejado', 'Confirmado')
              AND p.data_agendada IS NOT NULL
              AND p.data_agendada >= @DataInicio
              AND p.data_agendada <= @DataFim
            ORDER BY p.data_agendada
            """;

        return await conn.QueryAsync<ProcedimentoCirurgicoResumoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio,
            DataFim = dataFim
        });
    }

    /// <summary>
    /// DTO intermediário para leitura Dapper. FichaAnestesica é JSONB — Npgsql retorna string
    /// ao usar ::text; deserializamos após o mapeamento.
    /// </summary>
    private class ProcedimentoCirurgicoDtoRaw : ProcedimentoCirurgicoResumoDto
    {
        public string? DescricaoCirurgica { get; set; }
        public string? FichaAnestesicaRaw { get; set; }
        public string? EvolucaoPosOp { get; set; }
        public string? Observacoes { get; set; }
        public DateTime? CanceladoEm { get; set; }
        public string? MotivoCancelamento { get; set; }
        public DateTime? AtualizadoEm { get; set; }

        public ProcedimentoCirurgicoDto ToDto() => new()
        {
            Id = Id,
            EstabelecimentoId = EstabelecimentoId,
            PacienteId = PacienteId,
            PacienteNome = PacienteNome,
            ProntuarioId = ProntuarioId,
            AgendamentoId = AgendamentoId,
            DataAgendada = DataAgendada,
            DataRealizada = DataRealizada,
            Status = Status,
            CirurgiaPrincipal = CirurgiaPrincipal,
            CirurgiaCodigo = CirurgiaCodigo,
            CriadoEm = CriadoEm,
            DescricaoCirurgica = DescricaoCirurgica,
            FichaAnestesica = FichaAnestesicaRaw is null
                ? null
                : JsonSerializer.Deserialize<FichaAnestesica>(FichaAnestesicaRaw),
            EvolucaoPosOp = EvolucaoPosOp,
            Observacoes = Observacoes,
            CanceladoEm = CanceladoEm,
            MotivoCancelamento = MotivoCancelamento,
            AtualizadoEm = AtualizadoEm,
        };
    }
}
