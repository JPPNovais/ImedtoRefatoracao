using Dapper;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories.OrcamentoCatalogos;

/// <summary>
/// Read-side dos catálogos de orçamento. Listagens compactas para a tela de Settings.
/// </summary>
public class OrcamentoCatalogoQueryRepository
{
    private readonly string _connStr;
    public OrcamentoCatalogoQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<IEnumerable<CatalogoCirurgiaDto>> ListarCirurgias(long estabelecimentoId, bool? ativas)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT id AS Id, estabelecimento_id AS EstabelecimentoId, descricao AS Descricao,
                   valor_base AS ValorBase, duracao_padrao_minutos AS DuracaoPadraoMinutos,
                   ativo AS Ativo, criada_em AS CriadaEm, atualizada_em AS AtualizadaEm
            FROM orcamento_catalogo_cirurgia
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@Ativas::boolean IS NULL OR ativo = @Ativas::boolean)
            ORDER BY descricao
            """;
        return await conn.QueryAsync<CatalogoCirurgiaDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativas = ativas });
    }

    public async Task<IEnumerable<ValorProfissionalOrcamentoDto>> ListarValoresProfissional(long estabelecimentoId, bool? ativos)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT v.id AS Id, v.estabelecimento_id AS EstabelecimentoId,
                   v.profissional_usuario_id AS ProfissionalUsuarioId,
                   COALESCE(u.nome_completo, u.email) AS ProfissionalNome,
                   v.funcao AS Funcao,
                   v.tempo_base_minutos AS TempoBaseMinutos,
                   v.valor_tempo_base AS ValorTempoBase,
                   v.tempo_adicional_minutos AS TempoAdicionalMinutos,
                   v.valor_adicional AS ValorAdicional,
                   v.valor_plus AS ValorPlus,
                   v.ativo AS Ativo, v.criada_em AS CriadaEm, v.atualizada_em AS AtualizadaEm
            FROM orcamento_valor_profissional v
            LEFT JOIN usuarios u ON u.id = v.profissional_usuario_id
            WHERE v.estabelecimento_id = @EstabelecimentoId
              AND (@Ativos::boolean IS NULL OR v.ativo = @Ativos::boolean)
            ORDER BY v.funcao, ProfissionalNome NULLS FIRST
            """;
        return await conn.QueryAsync<ValorProfissionalOrcamentoDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativos = ativos });
    }

    public async Task<IEnumerable<ConfiguracaoLocalCirurgiaDto>> ListarConfiguracoesLocal(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT id AS Id, estabelecimento_id AS EstabelecimentoId,
                   tipo_internacao AS TipoInternacao,
                   tempo_base_minutos AS TempoBaseMinutos, valor_base AS ValorBase,
                   tempo_adicional_minutos AS TempoAdicionalMinutos, valor_adicional AS ValorAdicional,
                   criada_em AS CriadaEm, atualizada_em AS AtualizadaEm
            FROM orcamento_configuracao_local_cirurgia
            WHERE estabelecimento_id = @EstabelecimentoId
            ORDER BY tipo_internacao
            """;
        return await conn.QueryAsync<ConfiguracaoLocalCirurgiaDto>(sql, new { EstabelecimentoId = estabelecimentoId });
    }

    public async Task<IEnumerable<CatalogoEquipeEspecializadaDto>> ListarEquipes(long estabelecimentoId, bool? ativas)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT id AS Id, estabelecimento_id AS EstabelecimentoId, descricao AS Descricao,
                   valor_padrao AS ValorPadrao,
                   ativo AS Ativo, criada_em AS CriadaEm, atualizada_em AS AtualizadaEm
            FROM orcamento_catalogo_equipe
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@Ativas::boolean IS NULL OR ativo = @Ativas::boolean)
            ORDER BY descricao
            """;
        return await conn.QueryAsync<CatalogoEquipeEspecializadaDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativas = ativas });
    }

    public async Task<IEnumerable<CatalogoImplanteDto>> ListarImplantes(long estabelecimentoId, bool? ativos)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT i.id AS Id, i.estabelecimento_id AS EstabelecimentoId,
                   i.item_inventario_id AS ItemInventarioId,
                   inv.nome AS ItemInventarioNome,
                   i.descricao AS Descricao, i.custo_unitario AS CustoUnitario,
                   i.ativo AS Ativo, i.criada_em AS CriadaEm, i.atualizada_em AS AtualizadaEm
            FROM orcamento_catalogo_implante i
            LEFT JOIN itens_inventario inv ON inv.id = i.item_inventario_id
            WHERE i.estabelecimento_id = @EstabelecimentoId
              AND (@Ativos::boolean IS NULL OR i.ativo = @Ativos::boolean)
            ORDER BY i.descricao
            """;
        return await conn.QueryAsync<CatalogoImplanteDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativos = ativos });
    }

    public async Task<IEnumerable<CatalogoProdutoDto>> ListarProdutos(long estabelecimentoId, bool? ativos)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT id AS Id, estabelecimento_id AS EstabelecimentoId,
                   nome AS Nome, descricao AS Descricao,
                   valor_referencia AS ValorReferencia,
                   uso_unico AS UsoUnico,
                   ativo AS Ativo, criada_em AS CriadaEm, atualizada_em AS AtualizadaEm
            FROM orcamento_catalogo_produto
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@Ativos::boolean IS NULL OR ativo = @Ativos::boolean)
            ORDER BY nome
            """;
        return await conn.QueryAsync<CatalogoProdutoDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativos = ativos });
    }

    public async Task<IEnumerable<CatalogoCirurgiaProdutoDto>> ListarProdutosDaCirurgia(long catalogoCirurgiaId, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        // Filtro tenant via JOIN com catalogo_cirurgia (defesa em profundidade).
        const string sql = """
            SELECT cp.id AS Id,
                   cp.catalogo_cirurgia_id AS CatalogoCirurgiaId,
                   cp.catalogo_produto_id AS CatalogoProdutoId,
                   p.nome AS ProdutoNome,
                   p.uso_unico AS ProdutoUsoUnico,
                   p.valor_referencia AS ProdutoValorReferencia,
                   cp.quantidade_padrao AS QuantidadePadrao,
                   cp.obrigatorio AS Obrigatorio,
                   cp.criada_em AS CriadaEm
            FROM orcamento_catalogo_cirurgia_produto cp
            JOIN orcamento_catalogo_cirurgia c ON c.id = cp.catalogo_cirurgia_id
            JOIN orcamento_catalogo_produto p ON p.id = cp.catalogo_produto_id
            WHERE cp.catalogo_cirurgia_id = @CirurgiaId
              AND c.estabelecimento_id = @EstabelecimentoId
            ORDER BY p.nome
            """;
        return await conn.QueryAsync<CatalogoCirurgiaProdutoDto>(sql, new { CirurgiaId = catalogoCirurgiaId, EstabelecimentoId = estabelecimentoId });
    }

    public async Task<IEnumerable<ConfiguracaoPagamentoCatalogoDto>> ListarConfiguracoesPagamento(long estabelecimentoId, bool? ativas)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT c.id AS Id, c.estabelecimento_id AS EstabelecimentoId,
                   c.forma_pagamento_id AS FormaPagamentoId, fp.nome AS FormaPagamentoNome,
                   c.acrescimo_percentual AS AcrescimoPercentual,
                   c.entrada_percentual_padrao AS EntradaPercentualPadrao,
                   c.taxa_parcela AS TaxaParcela, c.parcelas_maximas AS ParcelasMaximas,
                   c.ativo AS Ativo, c.criada_em AS CriadaEm, c.atualizada_em AS AtualizadaEm
            FROM orcamento_configuracao_pagamento c
            LEFT JOIN formas_pagamento fp ON fp.id = c.forma_pagamento_id
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND (@Ativas::boolean IS NULL OR c.ativo = @Ativas::boolean)
            ORDER BY fp.nome
            """;
        return await conn.QueryAsync<ConfiguracaoPagamentoCatalogoDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativas = ativas });
    }
}
