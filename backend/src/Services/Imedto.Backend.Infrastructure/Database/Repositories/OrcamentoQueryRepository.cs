using System.Text.Json;
using Dapper;
using Imedto.Backend.Contracts.Orcamentos;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class OrcamentoQueryRepository
{
    private readonly string _connStr;

    /// <summary>
    /// Mesmas opções do <c>OrcamentoConfiguration</c>: PascalCase, sem indentação.
    /// Mantém leitura/escrita simétrica do jsonb.
    /// </summary>
    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public OrcamentoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<OrcamentoResumoDto>> Listar(
        long estabelecimentoId,
        long? pacienteId,
        string? status)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                o.id                    AS Id,
                o.estabelecimento_id    AS EstabelecimentoId,
                o.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                o.numero                AS Numero,
                o.status                AS Status,
                o.validade              AS Validade,
                COALESCE(SUM(i.subtotal), 0) AS Total,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em             AS CriadoEm,
                o.atualizado_em         AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            LEFT JOIN itens_orcamento i ON i.orcamento_id = o.id
            WHERE o.estabelecimento_id = @EstabelecimentoId
              AND (@PacienteId::bigint IS NULL OR o.paciente_id = @PacienteId::bigint)
              AND (@Status::text      IS NULL OR o.status = @Status::text)
            GROUP BY o.id, pac.nome_completo, u.nome_completo, u.email
            ORDER BY o.criado_em DESC
            """;

        return await conn.QueryAsync<OrcamentoResumoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Status = status
        });
    }

    public async Task<OrcamentoDto?> ObterPorId(long id)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlOrc = """
            SELECT
                o.id                    AS Id,
                o.estabelecimento_id    AS EstabelecimentoId,
                o.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                o.numero                AS Numero,
                o.status                AS Status,
                o.validade              AS Validade,
                o.observacoes           AS Observacoes,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em             AS CriadoEm,
                o.atualizado_em         AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            WHERE o.id = @Id
            """;

        const string sqlItens = """
            SELECT
                id              AS Id,
                descricao       AS Descricao,
                quantidade      AS Quantidade,
                valor_unitario  AS ValorUnitario,
                desconto_percent AS DescontoPercent,
                subtotal        AS Subtotal
            FROM itens_orcamento
            WHERE orcamento_id = @Id
            ORDER BY id
            """;

        var orc = await conn.QuerySingleOrDefaultAsync<OrcamentoDto>(sqlOrc, new { Id = id });
        if (orc is null) return null;

        var itens = await conn.QueryAsync<ItemOrcamentoDto>(sqlItens, new { Id = id });
        orc.Itens = itens.ToList();
        orc.Total = orc.Itens.Sum(i => i.Subtotal);

        return orc;
    }

    /// <summary>
    /// Multi-mapping para o orçamento completo (item 3.3.B + item 6): root + itens +
    /// equipe + implantes + formas + cirurgias + internação + anestesia. Faz uma query
    /// por collection/relação para evitar produto cartesiano.
    ///
    /// Item 7: <c>config_pagamento_json</c> é desserializado para o POCO
    /// <see cref="ConfigPagamentoOrcamentoDto"/> em vez de retornar a string opaca.
    /// </summary>
    public async Task<OrcamentoCompletoDto?> ObterCompletoPorId(long id)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlOrc = """
            SELECT
                o.id                          AS Id,
                o.estabelecimento_id          AS EstabelecimentoId,
                o.paciente_id                 AS PacienteId,
                pac.nome_completo             AS PacienteNome,
                o.numero                      AS Numero,
                o.status                      AS Status,
                o.validade                    AS Validade,
                o.observacoes                 AS Observacoes,
                o.tipo                        AS Tipo,
                o.procedimento_cirurgico_id   AS ProcedimentoCirurgicoId,
                o.config_pagamento_json::text AS ConfigPagamentoRaw,
                o.custo_implantes_total       AS CustoImplantesTotal,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em                   AS CriadoEm,
                o.atualizado_em               AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            WHERE o.id = @Id
            """;

        const string sqlItens = """
            SELECT
                id              AS Id,
                descricao       AS Descricao,
                quantidade      AS Quantidade,
                valor_unitario  AS ValorUnitario,
                desconto_percent AS DescontoPercent,
                subtotal        AS Subtotal
            FROM itens_orcamento
            WHERE orcamento_id = @Id
            ORDER BY id
            """;

        const string sqlEquipe = """
            SELECT
                e.id                       AS Id,
                e.profissional_usuario_id  AS ProfissionalUsuarioId,
                COALESCE(u.nome_completo, u.email) AS ProfissionalNome,
                e.papel                    AS Papel,
                e.valor                    AS Valor,
                e.ordem                    AS Ordem
            FROM orcamento_equipe e
            LEFT JOIN usuarios u ON u.id = e.profissional_usuario_id
            WHERE e.orcamento_id = @Id
            ORDER BY e.ordem, e.id
            """;

        const string sqlImplantes = """
            SELECT
                id                  AS Id,
                item_inventario_id  AS ItemInventarioId,
                descricao           AS Descricao,
                quantidade          AS Quantidade,
                custo_unitario      AS CustoUnitario,
                custo_total         AS CustoTotal
            FROM orcamento_implantes
            WHERE orcamento_id = @Id
            ORDER BY id
            """;

        const string sqlFormas = """
            SELECT
                f.id                    AS Id,
                f.forma_pagamento_id    AS FormaPagamentoId,
                fp.nome                 AS FormaPagamentoNome,
                f.valor                 AS Valor,
                f.parcelas              AS Parcelas,
                f.acrescimo_percentual  AS AcrescimoPercentual,
                f.entrada_percentual    AS EntradaPercentual,
                f.observacao            AS Observacao,
                f.ordem                 AS Ordem
            FROM orcamento_formas_pagamento f
            LEFT JOIN formas_pagamento fp ON fp.id = f.forma_pagamento_id
            WHERE f.orcamento_id = @Id
            ORDER BY f.ordem, f.id
            """;

        const string sqlCirurgias = """
            SELECT
                id                          AS Id,
                procedimento_cirurgico_id   AS ProcedimentoCirurgicoId,
                descricao                   AS Descricao,
                quantidade                  AS Quantidade,
                duracao_minutos             AS DuracaoMinutos,
                valor_total                 AS ValorTotal,
                ordem                       AS Ordem
            FROM orcamento_cirurgias
            WHERE orcamento_id = @Id
            ORDER BY ordem, id
            """;

        const string sqlInternacao = """
            SELECT
                tipo_internacao  AS TipoInternacao,
                dias             AS Dias,
                valor_diaria     AS ValorDiaria,
                valor_total      AS ValorTotal
            FROM orcamento_internacao
            WHERE orcamento_id = @Id
            """;

        const string sqlAnestesia = """
            SELECT
                tipo_anestesia AS TipoAnestesia,
                valor          AS Valor,
                observacao     AS Observacao
            FROM orcamento_anestesia
            WHERE orcamento_id = @Id
            """;

        var raw = await conn.QuerySingleOrDefaultAsync<OrcamentoCompletoRowRaw>(sqlOrc, new { Id = id });
        if (raw is null) return null;

        var orc = new OrcamentoCompletoDto
        {
            Id = raw.Id,
            EstabelecimentoId = raw.EstabelecimentoId,
            PacienteId = raw.PacienteId,
            PacienteNome = raw.PacienteNome ?? string.Empty,
            Numero = raw.Numero ?? string.Empty,
            Status = raw.Status ?? string.Empty,
            Validade = raw.Validade,
            Observacoes = raw.Observacoes,
            Tipo = raw.Tipo ?? "Simples",
            ProcedimentoCirurgicoId = raw.ProcedimentoCirurgicoId,
            Configuracao = DesserializarConfig(raw.ConfigPagamentoRaw),
            CustoImplantesTotal = raw.CustoImplantesTotal,
            CriadoPorNome = raw.CriadoPorNome ?? string.Empty,
            CriadoEm = raw.CriadoEm,
            AtualizadoEm = raw.AtualizadoEm
        };

        var itens = await conn.QueryAsync<ItemOrcamentoDto>(sqlItens, new { Id = id });
        var equipe = await conn.QueryAsync<OrcamentoEquipeDto>(sqlEquipe, new { Id = id });
        var implantes = await conn.QueryAsync<OrcamentoImplanteDto>(sqlImplantes, new { Id = id });
        var formas = await conn.QueryAsync<OrcamentoFormaPagamentoDto>(sqlFormas, new { Id = id });
        var cirurgias = await conn.QueryAsync<OrcamentoCirurgiaDto>(sqlCirurgias, new { Id = id });
        var internacao = await conn.QuerySingleOrDefaultAsync<OrcamentoInternacaoDto>(sqlInternacao, new { Id = id });
        var anestesia = await conn.QuerySingleOrDefaultAsync<OrcamentoAnestesiaDto>(sqlAnestesia, new { Id = id });

        orc.Itens = itens.ToList();
        orc.Equipe = equipe.ToList();
        orc.Implantes = implantes.ToList();
        orc.FormasPagamento = formas.ToList();
        orc.Cirurgias = cirurgias.ToList();
        orc.Internacao = internacao;
        orc.Anestesia = anestesia;

        // Total bruto: itens + implantes + comissões + cirurgias + internação + anestesia.
        // Espelha exatamente o getter Orcamento.Total no domínio.
        orc.Total = orc.Itens.Sum(i => i.Subtotal)
                  + orc.Implantes.Sum(i => i.CustoTotal)
                  + orc.Equipe.Sum(e => e.Valor)
                  + orc.Cirurgias.Sum(c => c.ValorTotal)
                  + (orc.Internacao?.ValorTotal ?? 0m)
                  + (orc.Anestesia?.Valor ?? 0m);

        return orc;
    }

    private static ConfigPagamentoOrcamentoDto? DesserializarConfig(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            return JsonSerializer.Deserialize<ConfigPagamentoOrcamentoDto>(raw, ConfigJsonOptions);
        }
        catch (JsonException)
        {
            // Backward-compat: configs antigas em formato livre são ignoradas — devolvendo
            // null evita 500 ao listar orçamentos pré-item-7. O front não perde nada
            // crítico (entrada/juros migraram para a tabela de formas).
            return null;
        }
    }

    /// <summary>
    /// Linha bruta do orçamento — separamos o jsonb como string (<c>::text</c>) para
    /// permitir desserialização manual com tratamento de erro.
    /// </summary>
    private sealed class OrcamentoCompletoRowRaw
    {
        public long Id { get; set; }
        public long EstabelecimentoId { get; set; }
        public long PacienteId { get; set; }
        public string? PacienteNome { get; set; }
        public string? Numero { get; set; }
        public string? Status { get; set; }
        public DateOnly Validade { get; set; }
        public string? Observacoes { get; set; }
        public string? Tipo { get; set; }
        public long? ProcedimentoCirurgicoId { get; set; }
        public string? ConfigPagamentoRaw { get; set; }
        public decimal CustoImplantesTotal { get; set; }
        public string? CriadoPorNome { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
