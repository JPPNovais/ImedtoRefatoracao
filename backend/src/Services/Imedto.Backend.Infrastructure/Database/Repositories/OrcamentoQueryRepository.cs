using Dapper;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side do orçamento (Dapper). Aggregate único — todas as collections são
/// carregadas em queries separadas por orçamento (split queries) para evitar produto
/// cartesiano. Não há mais distinção "obter resumo vs completo" — o DTO único traz
/// tudo que o front precisa renderizar.
/// </summary>
public class OrcamentoQueryRepository
{
    private readonly string _connStr;

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
                o.titulo                AS Titulo,
                o.status                AS Status,
                o.validade              AS Validade,
                o.agendamento_id        AS AgendamentoId,
                (
                    COALESCE((SELECT SUM(subtotal)    FROM itens_orcamento         WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(custo_total) FROM orcamento_implantes     WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(valor)       FROM orcamento_equipe        WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(valor_total) FROM orcamento_cirurgias     WHERE orcamento_id = o.id), 0)
                  + COALESCE(o.valor_local, 0)
                  + COALESCE((SELECT valor            FROM orcamento_anestesia     WHERE orcamento_id = o.id), 0)
                ) AS Total,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em             AS CriadoEm,
                o.atualizado_em         AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            WHERE o.estabelecimento_id = @EstabelecimentoId
              AND (@PacienteId::bigint IS NULL OR o.paciente_id = @PacienteId::bigint)
              AND (@Status::text      IS NULL OR o.status = @Status::text)
            ORDER BY o.criado_em DESC
            """;

        return await conn.QueryAsync<OrcamentoResumoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            PacienteId = pacienteId,
            Status = status
        });
    }

    public async Task<OrcamentoResumoDto?> ObterResumoPorAgendamento(long agendamentoId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT
                o.id                    AS Id,
                o.estabelecimento_id    AS EstabelecimentoId,
                o.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                o.numero                AS Numero,
                o.titulo                AS Titulo,
                o.status                AS Status,
                o.validade              AS Validade,
                o.agendamento_id        AS AgendamentoId,
                (
                    COALESCE((SELECT SUM(subtotal)    FROM itens_orcamento         WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(custo_total) FROM orcamento_implantes     WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(valor)       FROM orcamento_equipe        WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(valor_total) FROM orcamento_cirurgias     WHERE orcamento_id = o.id), 0)
                  + COALESCE(o.valor_local, 0)
                  + COALESCE((SELECT valor            FROM orcamento_anestesia     WHERE orcamento_id = o.id), 0)
                ) AS Total,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em             AS CriadoEm,
                o.atualizado_em         AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            WHERE o.agendamento_id     = @AgendamentoId
              AND o.estabelecimento_id = @EstabelecimentoId
              AND o.status NOT IN ('Cancelado','Recusado')
            ORDER BY o.criado_em DESC
            LIMIT 1
            """;
        return await conn.QuerySingleOrDefaultAsync<OrcamentoResumoDto>(sql, new
        {
            AgendamentoId = agendamentoId,
            EstabelecimentoId = estabelecimentoId
        });
    }

    public async Task<OrcamentoDto?> ObterPorId(long id, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sqlOrc = """
            SELECT
                o.id                          AS Id,
                o.estabelecimento_id          AS EstabelecimentoId,
                o.paciente_id                 AS PacienteId,
                pac.nome_completo             AS PacienteNome,
                o.numero                      AS Numero,
                o.titulo                      AS Titulo,
                o.status                      AS Status,
                o.validade                    AS Validade,
                o.agendamento_id              AS AgendamentoId,
                o.observacoes                 AS Observacoes,
                o.procedimento_cirurgico_id   AS ProcedimentoCirurgicoId,
                o.custo_implantes_total       AS CustoImplantesTotal,
                o.tipo_local                  AS TipoLocal,
                o.tempo_local_minutos         AS TempoLocalMinutos,
                o.valor_local                 AS ValorLocal,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                o.criado_em                   AS CriadoEm,
                o.atualizado_em               AS AtualizadoEm
            FROM orcamentos o
            JOIN pacientes pac ON pac.id = o.paciente_id
            JOIN usuarios   u   ON u.id  = o.criado_por_usuario_id
            WHERE o.id = @Id
              AND o.estabelecimento_id = @EstabelecimentoId
            """;

        const string sqlItens = """
            SELECT id AS Id, descricao AS Descricao, quantidade AS Quantidade,
                   valor_unitario AS ValorUnitario, desconto_percent AS DescontoPercent,
                   subtotal AS Subtotal
            FROM itens_orcamento WHERE orcamento_id = @Id ORDER BY id
            """;

        const string sqlEquipe = """
            SELECT e.id AS Id, e.profissional_usuario_id AS ProfissionalUsuarioId,
                   COALESCE(u.nome_completo, u.email) AS ProfissionalNome,
                   e.papel AS Papel, e.valor AS Valor, e.ordem AS Ordem
            FROM orcamento_equipe e
            LEFT JOIN usuarios u ON u.id = e.profissional_usuario_id
            WHERE e.orcamento_id = @Id ORDER BY e.ordem, e.id
            """;

        const string sqlImplantes = """
            SELECT id AS Id, item_inventario_id AS ItemInventarioId, descricao AS Descricao,
                   quantidade AS Quantidade, custo_unitario AS CustoUnitario, custo_total AS CustoTotal
            FROM orcamento_implantes WHERE orcamento_id = @Id ORDER BY id
            """;

        const string sqlFormas = """
            SELECT f.id AS Id, f.forma_pagamento_id AS FormaPagamentoId,
                   fp.nome AS FormaPagamentoNome, f.valor AS Valor, f.parcelas AS Parcelas,
                   f.acrescimo_percentual AS AcrescimoPercentual, f.entrada_percentual AS EntradaPercentual,
                   f.observacao AS Observacao, f.ordem AS Ordem
            FROM orcamento_formas_pagamento f
            LEFT JOIN formas_pagamento fp ON fp.id = f.forma_pagamento_id
            WHERE f.orcamento_id = @Id ORDER BY f.ordem, f.id
            """;

        const string sqlCirurgias = """
            SELECT id AS Id, procedimento_cirurgico_id AS ProcedimentoCirurgicoId,
                   descricao AS Descricao, quantidade AS Quantidade,
                   duracao_minutos AS DuracaoMinutos, valor_total AS ValorTotal, ordem AS Ordem
            FROM orcamento_cirurgias WHERE orcamento_id = @Id ORDER BY ordem, id
            """;

        const string sqlAnestesia = """
            SELECT tipo_anestesia AS TipoAnestesia, valor AS Valor, observacao AS Observacao
            FROM orcamento_anestesia WHERE orcamento_id = @Id
            """;

        var orcRaw = await conn.QuerySingleOrDefaultAsync<OrcamentoRawDto>(sqlOrc, new
        {
            Id = id,
            EstabelecimentoId = estabelecimentoId
        });
        if (orcRaw is null) return null;

        var orc = new OrcamentoDto
        {
            Id = orcRaw.Id,
            EstabelecimentoId = orcRaw.EstabelecimentoId,
            PacienteId = orcRaw.PacienteId,
            PacienteNome = orcRaw.PacienteNome,
            Numero = orcRaw.Numero,
            Titulo = orcRaw.Titulo,
            Status = orcRaw.Status,
            Validade = orcRaw.Validade,
            AgendamentoId = orcRaw.AgendamentoId,
            Observacoes = orcRaw.Observacoes,
            ProcedimentoCirurgicoId = orcRaw.ProcedimentoCirurgicoId,
            CustoImplantesTotal = orcRaw.CustoImplantesTotal,
            CriadoPorNome = orcRaw.CriadoPorNome,
            CriadoEm = orcRaw.CriadoEm,
            AtualizadoEm = orcRaw.AtualizadoEm,
        };

        if (!string.IsNullOrEmpty(orcRaw.TipoLocal))
        {
            orc.LocalCirurgia = new OrcamentoLocalCirurgiaDto
            {
                Tipo = orcRaw.TipoLocal,
                TempoMinutos = orcRaw.TempoLocalMinutos ?? 0,
                Valor = orcRaw.ValorLocal,
            };
        }

        var itens = await conn.QueryAsync<ItemOrcamentoDto>(sqlItens, new { Id = id });
        var equipe = await conn.QueryAsync<OrcamentoEquipeDto>(sqlEquipe, new { Id = id });
        var implantes = await conn.QueryAsync<OrcamentoImplanteDto>(sqlImplantes, new { Id = id });
        var formas = await conn.QueryAsync<OrcamentoFormaPagamentoDto>(sqlFormas, new { Id = id });
        var cirurgias = await conn.QueryAsync<OrcamentoCirurgiaDto>(sqlCirurgias, new { Id = id });
        var anestesia = await conn.QuerySingleOrDefaultAsync<OrcamentoAnestesiaDto>(sqlAnestesia, new { Id = id });

        orc.Itens = itens.ToList();
        orc.Equipe = equipe.ToList();
        orc.Implantes = implantes.ToList();
        orc.FormasPagamento = formas.ToList();
        orc.Cirurgias = cirurgias.ToList();
        orc.Anestesia = anestesia;

        orc.Total = orc.Itens.Sum(i => i.Subtotal)
                  + orc.Implantes.Sum(i => i.CustoTotal)
                  + orc.Equipe.Sum(e => e.Valor)
                  + orc.Cirurgias.Sum(c => c.ValorTotal)
                  + (orc.LocalCirurgia?.Valor ?? 0m)
                  + (orc.Anestesia?.Valor ?? 0m);

        return orc;
    }

    /// <summary>Linha bruta usada para hidratar OrcamentoDto preservando os campos do local cirúrgico.</summary>
    private class OrcamentoRawDto
    {
        public long Id { get; set; }
        public long EstabelecimentoId { get; set; }
        public long PacienteId { get; set; }
        public string PacienteNome { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string? Titulo { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly Validade { get; set; }
        public long? AgendamentoId { get; set; }
        public string? Observacoes { get; set; }
        public long? ProcedimentoCirurgicoId { get; set; }
        public decimal CustoImplantesTotal { get; set; }
        public string? TipoLocal { get; set; }
        public int? TempoLocalMinutos { get; set; }
        public decimal ValorLocal { get; set; }
        public string CriadoPorNome { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
