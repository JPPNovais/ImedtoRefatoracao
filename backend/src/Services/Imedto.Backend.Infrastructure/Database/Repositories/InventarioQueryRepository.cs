using Dapper;
using Imedto.Backend.Contracts.Inventario.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class InventarioQueryRepository
{
    private readonly string _connStr;

    public InventarioQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaItensInventarioDto> ListarItens(
        long estabelecimentoId,
        string? categoria,
        bool? apenasAbaixoMinimo,
        bool? apenasAtivos,
        int pagina,
        int tamanhoPagina)
    {
        if (pagina < 1) throw new BusinessException("Página deve ser maior ou igual a 1.");
        if (tamanhoPagina < 1 || tamanhoPagina > 100)
            throw new BusinessException("Tamanho da página deve estar entre 1 e 100.");

        var offset = (pagina - 1) * tamanhoPagina;

        await using var conn = new NpgsqlConnection(_connStr);

        // SELECT minimizado (LGPD): só os campos que a tela usa.
        // Joins LEFT pra suportar período de deprecation (CategoriaEstoque obrigatória,
        // mas Fabricante/Fornecedor/Local são opcionais).
        const string sql = """
            SELECT count(*)
            FROM   itens_inventario i
            WHERE  i.estabelecimento_id = @EstabelecimentoId
              AND  (@Categoria::text          IS NULL OR i.categoria = @Categoria::text)
              AND  (@ApenasAbaixoMinimo::boolean IS NULL OR NOT @ApenasAbaixoMinimo::boolean OR i.quantidade_atual < i.quantidade_minima)
              AND  (@ApenasAtivos::boolean       IS NULL OR i.ativo = @ApenasAtivos::boolean);

            SELECT
                i.id                                        AS Id,
                i.codigo                                    AS Codigo,
                i.nome                                      AS Nome,
                i.categoria                                 AS Categoria,
                i.categoria_id                              AS CategoriaId,
                c.cor                                       AS CategoriaCor,
                c.icone                                     AS CategoriaIcone,
                i.fabricante_id                             AS FabricanteId,
                fb.nome                                     AS FabricanteNome,
                i.fornecedor_padrao_id                      AS FornecedorPadraoId,
                fn.razao_social                             AS FornecedorPadraoNome,
                i.local_padrao_id                           AS LocalPadraoId,
                lc.nome                                     AS LocalPadraoNome,
                i.unidade_medida                            AS UnidadeMedida,
                i.quantidade_atual                          AS QuantidadeAtual,
                i.quantidade_minima                         AS QuantidadeMinima,
                i.custo_medio                               AS CustoMedio,
                i.custo_unitario                            AS CustoUnitario,
                (i.quantidade_atual < i.quantidade_minima)  AS EstoqueAbaixoMinimo,
                i.ativo                                     AS Ativo,
                i.criado_em                                 AS CriadoEm
            FROM itens_inventario i
            LEFT JOIN categorias_estoque    c  ON c.id  = i.categoria_id          AND c.estabelecimento_id  = i.estabelecimento_id
            LEFT JOIN fabricantes_estoque   fb ON fb.id = i.fabricante_id         AND fb.estabelecimento_id = i.estabelecimento_id
            LEFT JOIN fornecedores_estoque  fn ON fn.id = i.fornecedor_padrao_id  AND fn.estabelecimento_id = i.estabelecimento_id
            LEFT JOIN locais_estoque        lc ON lc.id = i.local_padrao_id       AND lc.estabelecimento_id = i.estabelecimento_id
            WHERE i.estabelecimento_id = @EstabelecimentoId
              AND (@Categoria::text          IS NULL OR i.categoria = @Categoria::text)
              AND (@ApenasAbaixoMinimo::boolean IS NULL OR NOT @ApenasAbaixoMinimo::boolean OR i.quantidade_atual < i.quantidade_minima)
              AND (@ApenasAtivos::boolean       IS NULL OR i.ativo = @ApenasAtivos::boolean)
            ORDER BY i.categoria, i.nome
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var parametros = new
        {
            EstabelecimentoId = estabelecimentoId,
            Categoria = categoria,
            ApenasAbaixoMinimo = apenasAbaixoMinimo,
            ApenasAtivos = apenasAtivos,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, parametros);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ItemInventarioDto>();

        return new PaginaItensInventarioDto
        {
            Itens = itens.ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }

    public async Task<PaginaMovimentacoesEstoqueDto> ListarMovimentacoes(
        long estabelecimentoId,
        long? itemInventarioId,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        int pagina,
        int tamanhoPagina)
    {
        if (pagina < 1) throw new BusinessException("Página deve ser maior ou igual a 1.");
        if (tamanhoPagina < 1 || tamanhoPagina > 100)
            throw new BusinessException("Tamanho da página deve estar entre 1 e 100.");

        var offset = (pagina - 1) * tamanhoPagina;

        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT count(*)
            FROM   movimentacoes_estoque m
            WHERE  m.estabelecimento_id = @EstabelecimentoId
              AND  m.deletado_em IS NULL
              AND  (@ItemInventarioId::bigint IS NULL OR m.item_inventario_id = @ItemInventarioId::bigint)
              AND  (@DataInicio::timestamptz  IS NULL OR m.criado_em >= @DataInicio::timestamptz)
              AND  (@DataFim::timestamptz     IS NULL OR m.criado_em <= @DataFim::timestamptz);

            SELECT
                m.id                    AS Id,
                m.item_inventario_id    AS ItemInventarioId,
                i.nome                  AS ItemNome,
                m.tipo                  AS Tipo,
                m.quantidade            AS Quantidade,
                m.quantidade_anterior   AS QuantidadeAnterior,
                m.quantidade_apos       AS QuantidadeApos,
                m.custo_unitario        AS CustoUnitario,
                m.custo_total           AS CustoTotal,
                m.observacao            AS Observacao,
                COALESCE(u.nome_completo, u.email) AS UsuarioNome,
                m.criado_em             AS CriadoEm
            FROM movimentacoes_estoque m
            JOIN itens_inventario i ON i.id = m.item_inventario_id
            JOIN usuarios         u ON u.id = m.criado_por_usuario_id
            WHERE m.estabelecimento_id = @EstabelecimentoId
              AND m.deletado_em IS NULL
              AND (@ItemInventarioId::bigint IS NULL OR m.item_inventario_id = @ItemInventarioId::bigint)
              AND (@DataInicio::timestamptz  IS NULL OR m.criado_em >= @DataInicio::timestamptz)
              AND (@DataFim::timestamptz     IS NULL OR m.criado_em <= @DataFim::timestamptz)
            ORDER BY m.criado_em DESC
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var parametros = new
        {
            EstabelecimentoId = estabelecimentoId,
            ItemInventarioId = itemInventarioId,
            DataInicio = dataInicio.HasValue ? (DateTime?)dataInicio.Value.ToDateTime(TimeOnly.MinValue) : null,
            DataFim = dataFim.HasValue ? (DateTime?)dataFim.Value.ToDateTime(TimeOnly.MaxValue) : null,
            Tamanho = tamanhoPagina,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, parametros);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<MovimentacaoEstoqueDto>();

        return new PaginaMovimentacoesEstoqueDto
        {
            Itens = itens.ToList(),
            Total = total,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };
    }
}
