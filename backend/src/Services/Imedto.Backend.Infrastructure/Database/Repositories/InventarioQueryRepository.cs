using Dapper;
using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class InventarioQueryRepository
{
    private readonly string _connStr;

    public InventarioQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<ItemInventarioDto>> ListarItens(
        long estabelecimentoId,
        string? categoria,
        bool? apenasAbaixoMinimo,
        bool? apenasAtivos)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT
                i.id                                        AS Id,
                i.estabelecimento_id                        AS EstabelecimentoId,
                i.codigo                                    AS Codigo,
                i.nome                                      AS Nome,
                i.categoria                                 AS Categoria,
                i.unidade_medida                            AS UnidadeMedida,
                i.quantidade_atual                          AS QuantidadeAtual,
                i.quantidade_minima                         AS QuantidadeMinima,
                (i.quantidade_atual < i.quantidade_minima)  AS EstoqueAbaixoMinimo,
                i.ativo                                     AS Ativo,
                i.criado_em                                 AS CriadoEm,
                i.atualizado_em                             AS AtualizadoEm
            FROM itens_inventario i
            WHERE i.estabelecimento_id = @EstabelecimentoId
              AND (@Categoria::text          IS NULL OR i.categoria = @Categoria::text)
              AND (@ApenasAbaixoMinimo::boolean IS NULL OR NOT @ApenasAbaixoMinimo::boolean OR i.quantidade_atual < i.quantidade_minima)
              AND (@ApenasAtivos::boolean       IS NULL OR i.ativo = @ApenasAtivos::boolean)
            ORDER BY i.categoria, i.nome
            """;

        return await conn.QueryAsync<ItemInventarioDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            Categoria = categoria,
            ApenasAbaixoMinimo = apenasAbaixoMinimo,
            ApenasAtivos = apenasAtivos
        });
    }

    public async Task<IEnumerable<MovimentacaoEstoqueDto>> ListarMovimentacoes(
        long estabelecimentoId,
        long? itemInventarioId,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        int? limite)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var sql = $"""
            SELECT
                m.id                    AS Id,
                m.item_inventario_id    AS ItemInventarioId,
                i.nome                  AS ItemNome,
                m.tipo                  AS Tipo,
                m.quantidade            AS Quantidade,
                m.quantidade_anterior   AS QuantidadeAnterior,
                m.quantidade_apos       AS QuantidadeApos,
                m.observacao            AS Observacao,
                COALESCE(u.nome_completo, u.email) AS UsuarioNome,
                m.criado_em             AS CriadoEm
            FROM movimentacoes_estoque m
            JOIN itens_inventario i ON i.id = m.item_inventario_id
            JOIN usuarios         u ON u.id = m.criado_por_usuario_id
            WHERE m.estabelecimento_id = @EstabelecimentoId
              AND (@ItemInventarioId::bigint IS NULL OR m.item_inventario_id = @ItemInventarioId::bigint)
              AND (@DataInicio::timestamptz  IS NULL OR m.criado_em >= @DataInicio::timestamptz)
              AND (@DataFim::timestamptz     IS NULL OR m.criado_em <= @DataFim::timestamptz)
            ORDER BY m.criado_em DESC
            {(limite.HasValue ? $"LIMIT {limite.Value}" : "")}
            """;

        return await conn.QueryAsync<MovimentacaoEstoqueDto>(sql, new
        {
            EstabelecimentoId = estabelecimentoId,
            ItemInventarioId = itemInventarioId,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MaxValue) : null
        });
    }
}
