using Dapper;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public record AgendamentoParaDisponibilidade(DateTime InicioPrevisto, DateTime FimPrevisto, string PacienteNome);

public class AgendamentoQueryRepository
{
    private readonly string _connStr;
    private readonly IFotoStorageService _fotoStorage;

    public AgendamentoQueryRepository(AppReadConnectionString conn, IFotoStorageService fotoStorage)
    {
        _connStr = conn.Value;
        _fotoStorage = fotoStorage;
    }

    public async Task<PaginaAgendamentosDto> Listar(
        long estabelecimentoId,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        Guid? profissionalUsuarioId,
        long? pacienteId,
        string? status,
        int pagina,
        int tamanhoPagina)
    {
        if (pagina < 1) throw new BusinessException("Página deve ser maior ou igual a 1.");
        if (tamanhoPagina < 1 || tamanhoPagina > 100)
            throw new BusinessException("Tamanho da página deve estar entre 1 e 100.");

        var offset = (pagina - 1) * tamanhoPagina;

        await using var conn = new NpgsqlConnection(_connStr);

        // 2 queries num único round-trip via QueryMultiple — count + página.
        // Mantém SELECT minimizado e evita o overhead de COUNT(*) OVER() em janelas grandes.
        const string sql = """
            SELECT count(*)
            FROM   agendamentos a
            WHERE  a.estabelecimento_id = @EstabelecimentoId
              AND  (@DataInicio::timestamp           IS NULL OR a.inicio_previsto::date >= @DataInicio::date)
              AND  (@DataFim::timestamp              IS NULL OR a.inicio_previsto::date <= @DataFim::date)
              AND  (@ProfissionalUsuarioId::uuid     IS NULL OR a.profissional_usuario_id = @ProfissionalUsuarioId::uuid)
              AND  (@PacienteId::bigint              IS NULL OR a.paciente_id = @PacienteId::bigint)
              AND  (@Status::text                    IS NULL OR a.status = @Status::text);

            SELECT
                a.id                    AS Id,
                a.estabelecimento_id    AS EstabelecimentoId,
                a.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                a.profissional_usuario_id AS ProfissionalUsuarioId,
                COALESCE(uprf.nome_completo, uprf.email) AS ProfissionalNome,
                pprf.foto_url           AS ProfissionalFotoUrl,
                COALESCE(ucri.nome_completo, ucri.email) AS CriadoPorNome,
                a.inicio_previsto       AS InicioPrevisto,
                a.fim_previsto          AS FimPrevisto,
                a.tipo_servico          AS TipoServico,
                a.observacoes           AS Observacoes,
                a.status                AS Status,
                a.motivo_cancelamento   AS MotivoCancelamento,
                a.criado_em             AS CriadoEm,
                a.atualizado_em         AS AtualizadoEm,
                a.check_in_em           AS CheckInEm,
                a.sala_id               AS SalaId,
                sa.nome                 AS SalaNome,
                ts.nome                 AS SalaTipoNome
            FROM agendamentos a
            JOIN pacientes    pac  ON pac.id = a.paciente_id
            JOIN usuarios     uprf ON uprf.id = a.profissional_usuario_id
            JOIN usuarios     ucri ON ucri.id = a.criado_por_usuario_id
            LEFT JOIN profissionais pprf ON pprf.usuario_id = a.profissional_usuario_id AND pprf.deletado_em IS NULL
            LEFT JOIN sala_atendimento     sa ON sa.id = a.sala_id
            LEFT JOIN tipo_sala_atendimento ts ON ts.id = sa.tipo_sala_id
            WHERE a.estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::timestamp           IS NULL OR a.inicio_previsto::date >= @DataInicio::date)
              AND (@DataFim::timestamp              IS NULL OR a.inicio_previsto::date <= @DataFim::date)
              AND (@ProfissionalUsuarioId::uuid     IS NULL OR a.profissional_usuario_id = @ProfissionalUsuarioId::uuid)
              AND (@PacienteId::bigint              IS NULL OR a.paciente_id = @PacienteId::bigint)
              AND (@Status::text                    IS NULL OR a.status = @Status::text)
            ORDER BY a.inicio_previsto
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var parametros = new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MinValue) : null,
            ProfissionalUsuarioId = profissionalUsuarioId,
            PacienteId = pacienteId,
            Status = status,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, parametros);
        var total = await multi.ReadSingleAsync<int>();
        var itens = (await multi.ReadAsync<AgendamentoDto>()).ToList();

        foreach (var a in itens)
            a.ProfissionalFotoUrl = _fotoStorage.GerarUrlLeitura(a.ProfissionalFotoUrl);

        return new PaginaAgendamentosDto
        {
            Itens = itens,
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    public async Task<IEnumerable<ContagemPorDiaDto>> ContarPorDia(
        long estabelecimentoId,
        DateOnly dataInicio,
        DateOnly dataFim,
        Guid? profissionalUsuarioId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT  a.inicio_previsto::date AS Data,
                    COUNT(*)::int           AS Total
            FROM    agendamentos a
            WHERE   a.estabelecimento_id = @EstabelecimentoId
              AND   a.inicio_previsto::date >= @DataInicio::date
              AND   a.inicio_previsto::date <= @DataFim::date
              AND   (@ProfissionalUsuarioId::uuid IS NULL OR a.profissional_usuario_id = @ProfissionalUsuarioId::uuid)
            GROUP BY a.inicio_previsto::date
            """;

        return await conn.QueryAsync<ContagemPorDiaDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
            ProfissionalUsuarioId = profissionalUsuarioId,
        });
    }

    /// <summary>
    /// Retorna agendamentos ativos (não cancelados) de um profissional num intervalo de datas,
    /// com apenas os campos necessários para calcular disponibilidade.
    /// </summary>
    public async Task<IEnumerable<AgendamentoParaDisponibilidade>> ListarParaDisponibilidade(
        long estabelecimentoId,
        Guid profissionalUsuarioId,
        DateOnly dataInicio,
        DateOnly dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT  a.inicio_previsto   AS InicioPrevisto,
                    a.fim_previsto      AS FimPrevisto,
                    pac.nome_completo   AS PacienteNome
            FROM    agendamentos a
            JOIN    pacientes pac ON pac.id = a.paciente_id
            WHERE   a.estabelecimento_id = @EstabelecimentoId
              AND   a.profissional_usuario_id = @ProfissionalUsuarioId
              AND   a.status <> 'Cancelado'
              AND   a.inicio_previsto::date >= @DataInicio::date
              AND   a.inicio_previsto::date <= @DataFim::date
            ORDER BY a.inicio_previsto
            """;

        return await conn.QueryAsync<AgendamentoParaDisponibilidade>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            DataInicio = dataInicio.ToDateTime(TimeOnly.MinValue),
            DataFim = dataFim.ToDateTime(TimeOnly.MinValue),
        });
    }

    public async Task<AgendamentoDto?> ObterPorId(long id, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // Filtro por estabelecimento no SQL — defense-in-depth IDOR/LGPD.
        const string sql = """
            SELECT
                a.id                    AS Id,
                a.estabelecimento_id    AS EstabelecimentoId,
                a.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                a.profissional_usuario_id AS ProfissionalUsuarioId,
                COALESCE(uprf.nome_completo, uprf.email) AS ProfissionalNome,
                pprf.foto_url           AS ProfissionalFotoUrl,
                COALESCE(ucri.nome_completo, ucri.email) AS CriadoPorNome,
                a.inicio_previsto       AS InicioPrevisto,
                a.fim_previsto          AS FimPrevisto,
                a.tipo_servico          AS TipoServico,
                a.observacoes           AS Observacoes,
                a.status                AS Status,
                a.motivo_cancelamento   AS MotivoCancelamento,
                a.criado_em             AS CriadoEm,
                a.atualizado_em         AS AtualizadoEm,
                a.check_in_em           AS CheckInEm,
                a.sala_id               AS SalaId,
                sa.nome                 AS SalaNome,
                ts.nome                 AS SalaTipoNome
            FROM agendamentos a
            JOIN pacientes    pac  ON pac.id = a.paciente_id
            JOIN usuarios     uprf ON uprf.id = a.profissional_usuario_id
            JOIN usuarios     ucri ON ucri.id = a.criado_por_usuario_id
            LEFT JOIN profissionais pprf ON pprf.usuario_id = a.profissional_usuario_id AND pprf.deletado_em IS NULL
            LEFT JOIN sala_atendimento     sa ON sa.id = a.sala_id
            LEFT JOIN tipo_sala_atendimento ts ON ts.id = sa.tipo_sala_id
            WHERE a.id = @Id
              AND a.estabelecimento_id = @EstabelecimentoId
            """;

        var dto = await conn.QuerySingleOrDefaultAsync<AgendamentoDto>(sql, new
        {
            Id = id,
            EstabelecimentoId = estabelecimentoId
        });
        if (dto is not null)
            dto.ProfissionalFotoUrl = _fotoStorage.GerarUrlLeitura(dto.ProfissionalFotoUrl);
        return dto;
    }
}
