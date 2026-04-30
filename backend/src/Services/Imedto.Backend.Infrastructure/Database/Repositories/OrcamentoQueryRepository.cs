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

        // Soma direta dos totais materializados nas collections — evita carregar tudo
        // só para somar. Mantém a definição idêntica ao Orcamento.Total no domínio.
        const string sql = """
            SELECT
                o.id                    AS Id,
                o.estabelecimento_id    AS EstabelecimentoId,
                o.paciente_id           AS PacienteId,
                pac.nome_completo       AS PacienteNome,
                o.numero                AS Numero,
                o.status                AS Status,
                o.validade              AS Validade,
                (
                    COALESCE((SELECT SUM(subtotal)    FROM itens_orcamento         WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(custo_total) FROM orcamento_implantes     WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(valor)       FROM orcamento_equipe        WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT SUM(valor_total) FROM orcamento_cirurgias     WHERE orcamento_id = o.id), 0)
                  + COALESCE((SELECT valor_total      FROM orcamento_internacao    WHERE orcamento_id = o.id), 0)
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

    public async Task<OrcamentoDto?> ObterPorId(long id)
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
                o.procedimento_cirurgico_id   AS ProcedimentoCirurgicoId,
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

        var orc = await conn.QuerySingleOrDefaultAsync<OrcamentoDto>(sqlOrc, new { Id = id });
        if (orc is null) return null;

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
}
