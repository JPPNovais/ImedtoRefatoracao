using Dapper;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class FinanceiroQueryRepository
{
    private readonly string _connStr;

    public FinanceiroQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<LancamentoDto>> Listar(
        long estabelecimentoId,
        string? tipo,
        string? status,
        string? categoria,
        DateOnly? dataInicio,
        DateOnly? dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                l.id                    AS Id,
                l.estabelecimento_id    AS EstabelecimentoId,
                l.tipo                  AS Tipo,
                l.descricao             AS Descricao,
                l.valor                 AS Valor,
                l.data_vencimento       AS DataVencimento,
                l.data_pagamento        AS DataPagamento,
                l.status                AS Status,
                l.categoria             AS Categoria,
                l.orcamento_id          AS OrcamentoId,
                o.numero                AS OrcamentoNumero,
                COALESCE(u.nome_completo, u.email) AS CriadoPorNome,
                l.criado_em             AS CriadoEm
            FROM lancamentos l
            JOIN usuarios u ON u.id = l.criado_por_usuario_id
            LEFT JOIN orcamentos o ON o.id = l.orcamento_id
            WHERE l.estabelecimento_id = @EstabelecimentoId
              AND (@Tipo::text      IS NULL OR l.tipo = @Tipo::text)
              AND (@Status::text    IS NULL OR l.status = @Status::text)
              AND (@Categoria::text IS NULL OR l.categoria = @Categoria::text)
              AND (@DataInicio::date IS NULL OR l.data_vencimento >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR l.data_vencimento <= @DataFim::date)
            ORDER BY l.data_vencimento DESC, l.id DESC
            """;

        return await conn.QueryAsync<LancamentoDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            Status = status,
            Categoria = categoria,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MinValue) : null
        });
    }

    public async Task<ResumoFinanceiroDto> ObterResumo(
        long estabelecimentoId,
        DateOnly? dataInicio,
        DateOnly? dataFim)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                COALESCE(SUM(CASE WHEN tipo = 'Receita' AND status = 'Pago'     THEN valor ELSE 0 END), 0) AS TotalReceitasPagas,
                COALESCE(SUM(CASE WHEN tipo = 'Despesa' AND status = 'Pago'     THEN valor ELSE 0 END), 0) AS TotalDespesasPagas,
                COALESCE(SUM(CASE WHEN tipo = 'Receita' AND status = 'Pendente' THEN valor ELSE 0 END), 0) AS ReceitasPendentes,
                COALESCE(SUM(CASE WHEN tipo = 'Despesa' AND status = 'Pendente' THEN valor ELSE 0 END), 0) AS DespesasPendentes
            FROM lancamentos
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@DataInicio::date IS NULL OR data_vencimento >= @DataInicio::date)
              AND (@DataFim::date    IS NULL OR data_vencimento <= @DataFim::date)
            """;

        var resumo = await conn.QuerySingleAsync<ResumoFinanceiroDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MinValue) : null
        });

        resumo.Saldo = resumo.TotalReceitasPagas - resumo.TotalDespesasPagas;
        return resumo;
    }
}
