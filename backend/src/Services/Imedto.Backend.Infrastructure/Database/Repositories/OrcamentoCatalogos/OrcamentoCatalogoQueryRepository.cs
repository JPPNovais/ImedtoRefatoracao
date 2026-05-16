using Dapper;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories.OrcamentoCatalogos;

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
                   codigo_interno AS CodigoInterno, codigo_tuss AS CodigoTuss, categoria AS Categoria,
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
                   i.item_inventario_id AS ItemInventarioId, inv.nome AS ItemInventarioNome,
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
                   valor_referencia AS ValorReferencia, uso_unico AS UsoUnico,
                   tipo AS Tipo, marca AS Marca, unidade AS Unidade,
                   fornecedor_nome AS FornecedorNome, codigo_sku AS CodigoSku,
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
        const string sql = """
            SELECT cp.id AS Id,
                   cp.catalogo_cirurgia_id AS CatalogoCirurgiaId,
                   cp.catalogo_produto_id AS CatalogoProdutoId,
                   p.nome AS ProdutoNome, p.uso_unico AS ProdutoUsoUnico,
                   p.valor_referencia AS ProdutoValorReferencia,
                   cp.quantidade_padrao AS QuantidadePadrao,
                   cp.obrigatorio AS Obrigatorio,
                   cp.incluido AS Incluido,
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

    public async Task<IEnumerable<OrcamentoTeamRoleDto>> ListarTeamRoles(long estabelecimentoId, bool? ativos)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT t.id AS Id, t.estabelecimento_id AS EstabelecimentoId,
                   t.papel AS Papel,
                   t.profissional_usuario_id AS ProfissionalUsuarioId,
                   COALESCE(u.nome_completo, u.email) AS ProfissionalNome,
                   t.nome_padrao AS NomePadrao,
                   t.tipo_honorario AS TipoHonorario,
                   t.valor AS Valor, t.base_calculo AS BaseCalculo,
                   t.ativo AS Ativo, t.criada_em AS CriadaEm, t.atualizada_em AS AtualizadaEm
            FROM orcamento_team_role t
            LEFT JOIN usuarios u ON u.id = t.profissional_usuario_id
            WHERE t.estabelecimento_id = @EstabelecimentoId
              AND (@Ativos::boolean IS NULL OR t.ativo = @Ativos::boolean)
            ORDER BY t.papel, t.id
            """;
        return await conn.QueryAsync<OrcamentoTeamRoleDto>(sql, new { EstabelecimentoId = estabelecimentoId, Ativos = ativos });
    }

    public async Task<IEnumerable<OrcamentoAnestesistaDto>> ListarAnestesistas(long estabelecimentoId, bool? ativos)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sqlAnest = """
            SELECT id AS Id, estabelecimento_id AS EstabelecimentoId,
                   profissional_usuario_id AS ProfissionalUsuarioId,
                   nome AS Nome, crm AS Crm, especialidade AS Especialidade,
                   telefone AS Telefone, tabela_honorarios AS TabelaHonorarios,
                   ativo AS Ativo, criada_em AS CriadaEm, atualizada_em AS AtualizadaEm
            FROM orcamento_anestesista
            WHERE estabelecimento_id = @EstabelecimentoId
              AND (@Ativos::boolean IS NULL OR ativo = @Ativos::boolean)
            ORDER BY nome
            """;
        var anestesistas = (await conn.QueryAsync<OrcamentoAnestesistaDto>(sqlAnest,
            new { EstabelecimentoId = estabelecimentoId, Ativos = ativos })).ToList();

        if (anestesistas.Count == 0) return anestesistas;

        var ids = anestesistas.Select(a => a.Id).ToArray();
        const string sqlFaixas = """
            SELECT id AS id, anestesista_id AS anestesistaid,
                   descricao AS descricao, valor AS valor, ordem AS ordem
            FROM orcamento_anestesista_faixa
            WHERE anestesista_id = ANY(@Ids)
            ORDER BY anestesista_id, ordem
            """;
        var rows = await conn.QueryAsync<dynamic>(sqlFaixas, new { Ids = ids });
        var porAnestesista = rows
            .GroupBy(r => (long)r.anestesistaid)
            .ToDictionary(g => g.Key,
                g => g.Select(r => new OrcamentoAnestesistaFaixaDto
                {
                    Id = (long)r.id,
                    Descricao = (string)r.descricao,
                    Valor = (decimal)r.valor,
                    Ordem = (int)r.ordem
                }).ToList());

        foreach (var a in anestesistas)
            if (porAnestesista.TryGetValue(a.Id, out var faixas)) a.Faixas = faixas;

        return anestesistas;
    }

    public async Task<OrcamentoAnestesistaDto?> ObterAnestesista(long id, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sqlAnest = """
            SELECT id AS Id, estabelecimento_id AS EstabelecimentoId,
                   profissional_usuario_id AS ProfissionalUsuarioId,
                   nome AS Nome, crm AS Crm, especialidade AS Especialidade,
                   telefone AS Telefone, tabela_honorarios AS TabelaHonorarios,
                   ativo AS Ativo, criada_em AS CriadaEm, atualizada_em AS AtualizadaEm
            FROM orcamento_anestesista
            WHERE id = @Id AND estabelecimento_id = @EstabelecimentoId
            """;
        var anest = await conn.QuerySingleOrDefaultAsync<OrcamentoAnestesistaDto>(sqlAnest,
            new { Id = id, EstabelecimentoId = estabelecimentoId });
        if (anest is null) return null;

        const string sqlFaixas = """
            SELECT id AS Id, descricao AS Descricao, valor AS Valor, ordem AS Ordem
            FROM orcamento_anestesista_faixa
            WHERE anestesista_id = @Id
            ORDER BY ordem
            """;
        anest.Faixas = (await conn.QueryAsync<OrcamentoAnestesistaFaixaDto>(sqlFaixas, new { Id = id })).ToList();
        return anest;
    }

    public async Task<IEnumerable<OrcamentoPacoteResumoDto>> ListarPacotes(long estabelecimentoId, bool? ativos)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT p.id AS Id, p.estabelecimento_id AS EstabelecimentoId,
                   p.nome AS Nome, p.descricao AS Descricao,
                   p.anestesista_id AS AnestesistaId, a.nome AS AnestesistaNome,
                   p.valor_total_sugerido AS ValorTotalSugerido,
                   p.ativo AS Ativo,
                   (SELECT COUNT(*) FROM orcamento_pacote_procedimento pp WHERE pp.pacote_id = p.id) AS TotalProcedimentos,
                   (SELECT COUNT(*) FROM orcamento_pacote_produto pr WHERE pr.pacote_id = p.id) AS TotalProdutos,
                   (SELECT COUNT(*) FROM orcamento_pacote_team_role tr WHERE tr.pacote_id = p.id) AS TotalTeamRoles,
                   p.criada_em AS CriadaEm, p.atualizada_em AS AtualizadaEm
            FROM orcamento_pacote p
            LEFT JOIN orcamento_anestesista a ON a.id = p.anestesista_id
            WHERE p.estabelecimento_id = @EstabelecimentoId
              AND (@Ativos::boolean IS NULL OR p.ativo = @Ativos::boolean)
            ORDER BY p.nome
            """;
        return await conn.QueryAsync<OrcamentoPacoteResumoDto>(sql,
            new { EstabelecimentoId = estabelecimentoId, Ativos = ativos });
    }

    public async Task<OrcamentoPacoteDetalheDto?> ObterPacote(long id, long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sqlPacote = """
            SELECT p.id AS Id, p.estabelecimento_id AS EstabelecimentoId,
                   p.nome AS Nome, p.descricao AS Descricao,
                   p.anestesista_id AS AnestesistaId,
                   a.nome AS AnestesistaNome, a.ativo AS AnestesistaAtivo,
                   p.valor_total_sugerido AS ValorTotalSugerido,
                   p.ativo AS Ativo, p.criada_em AS CriadaEm, p.atualizada_em AS AtualizadaEm
            FROM orcamento_pacote p
            LEFT JOIN orcamento_anestesista a ON a.id = p.anestesista_id
            WHERE p.id = @Id AND p.estabelecimento_id = @EstabelecimentoId
            """;
        var pacote = await conn.QuerySingleOrDefaultAsync<OrcamentoPacoteDetalheDto>(sqlPacote,
            new { Id = id, EstabelecimentoId = estabelecimentoId });
        if (pacote is null) return null;

        const string sqlProcs = """
            SELECT pp.catalogo_cirurgia_id AS CatalogoCirurgiaId,
                   c.descricao AS Descricao, pp.ordem AS Ordem
            FROM orcamento_pacote_procedimento pp
            JOIN orcamento_catalogo_cirurgia c ON c.id = pp.catalogo_cirurgia_id
            WHERE pp.pacote_id = @Id
            ORDER BY pp.ordem
            """;
        pacote.Procedimentos = (await conn.QueryAsync<OrcamentoPacoteProcedimentoDto>(sqlProcs, new { Id = id })).ToList();

        const string sqlProds = """
            SELECT pr.catalogo_produto_id AS CatalogoProdutoId,
                   pp.nome AS Nome, pr.quantidade AS Quantidade
            FROM orcamento_pacote_produto pr
            JOIN orcamento_catalogo_produto pp ON pp.id = pr.catalogo_produto_id
            WHERE pr.pacote_id = @Id
            ORDER BY pp.nome
            """;
        pacote.Produtos = (await conn.QueryAsync<OrcamentoPacoteProdutoDto>(sqlProds, new { Id = id })).ToList();

        const string sqlRoles = """
            SELECT pt.team_role_id AS TeamRoleId, tr.papel AS Papel
            FROM orcamento_pacote_team_role pt
            JOIN orcamento_team_role tr ON tr.id = pt.team_role_id
            WHERE pt.pacote_id = @Id
            ORDER BY tr.papel
            """;
        pacote.TeamRoles = (await conn.QueryAsync<OrcamentoPacoteTeamRoleDto>(sqlRoles, new { Id = id })).ToList();

        return pacote;
    }
}
