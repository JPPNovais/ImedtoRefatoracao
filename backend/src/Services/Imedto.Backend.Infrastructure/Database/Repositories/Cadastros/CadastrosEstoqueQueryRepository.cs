using Dapper;
using Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories.Cadastros;

/// <summary>
/// Leituras Dapper dos 4 cadastros de estoque (categoria, fabricante, fornecedor, local).
/// Cada listagem retorna a quantidade de itens vinculados (sub-select count) — o uso é
/// raro o suficiente para não justificar pré-cálculo, e mais barato que o front fazer
/// queries paralelas por linha.
///
/// Multi-tenant: WHERE estabelecimento_id em todo SELECT — sem isso, qualquer bug
/// no controller vazaria dados entre tenants.
/// </summary>
public class CadastrosEstoqueQueryRepository
{
    private readonly string _connStr;

    public CadastrosEstoqueQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    // ───────────────────────── Categorias ─────────────────────────
    public async Task<PaginaCategoriasEstoqueDto> ListarCategorias(
        long estabelecimentoId, string? busca, bool? apenasAtivos, int pagina, int tamanho)
    {
        ValidarPagina(pagina, tamanho);
        var offset = (pagina - 1) * tamanho;

        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT count(*)
            FROM   categorias_estoque c
            WHERE  c.estabelecimento_id = @EstabelecimentoId
              AND  (@Busca::text IS NULL OR c.nome ILIKE '%' || @Busca::text || '%')
              AND  (@ApenasAtivos::boolean IS NULL OR c.ativo = @ApenasAtivos::boolean);

            SELECT
                c.id                AS Id,
                c.nome              AS Nome,
                c.cor               AS Cor,
                c.icone             AS Icone,
                c.ativo             AS Ativo,
                (SELECT count(*) FROM itens_inventario i
                    WHERE i.categoria_id = c.id AND i.estabelecimento_id = c.estabelecimento_id
                ) AS QuantidadeItens
            FROM categorias_estoque c
            WHERE c.estabelecimento_id = @EstabelecimentoId
              AND (@Busca::text IS NULL OR c.nome ILIKE '%' || @Busca::text || '%')
              AND (@ApenasAtivos::boolean IS NULL OR c.ativo = @ApenasAtivos::boolean)
            ORDER BY c.ativo DESC, lower(c.nome)
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            ApenasAtivos = apenasAtivos,
            Tamanho = tamanho,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<CategoriaEstoqueDto>();
        return new PaginaCategoriasEstoqueDto { Itens = itens.ToList(), Total = total, Pagina = pagina, TamanhoPagina = tamanho };
    }

    // ───────────────────────── Fabricantes ─────────────────────────
    public async Task<PaginaFabricantesEstoqueDto> ListarFabricantes(
        long estabelecimentoId, string? busca, bool? apenasAtivos, int pagina, int tamanho)
    {
        ValidarPagina(pagina, tamanho);
        var offset = (pagina - 1) * tamanho;

        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT count(*)
            FROM   fabricantes_estoque f
            WHERE  f.estabelecimento_id = @EstabelecimentoId
              AND  (@Busca::text IS NULL OR f.nome ILIKE '%' || @Busca::text || '%')
              AND  (@ApenasAtivos::boolean IS NULL OR f.ativo = @ApenasAtivos::boolean);

            SELECT
                f.id                AS Id,
                f.nome              AS Nome,
                f.pais              AS Pais,
                f.ativo             AS Ativo,
                (SELECT count(*) FROM itens_inventario i
                    WHERE i.fabricante_id = f.id AND i.estabelecimento_id = f.estabelecimento_id
                ) AS QuantidadeItens
            FROM fabricantes_estoque f
            WHERE f.estabelecimento_id = @EstabelecimentoId
              AND (@Busca::text IS NULL OR f.nome ILIKE '%' || @Busca::text || '%')
              AND (@ApenasAtivos::boolean IS NULL OR f.ativo = @ApenasAtivos::boolean)
            ORDER BY f.ativo DESC, lower(f.nome)
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            ApenasAtivos = apenasAtivos,
            Tamanho = tamanho,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<FabricanteEstoqueDto>();
        return new PaginaFabricantesEstoqueDto { Itens = itens.ToList(), Total = total, Pagina = pagina, TamanhoPagina = tamanho };
    }

    // ───────────────────────── Fornecedores ─────────────────────────
    public async Task<PaginaFornecedoresEstoqueDto> ListarFornecedores(
        long estabelecimentoId, string? busca, bool? apenasAtivos, int pagina, int tamanho)
    {
        ValidarPagina(pagina, tamanho);
        var offset = (pagina - 1) * tamanho;

        await using var conn = new NpgsqlConnection(_connStr);

        // Busca cruza razao_social e nome_fantasia (CNPJ não entra na busca livre
        // pra evitar enumeração — busca por CNPJ deveria ser exata, futuro).
        const string sql = """
            SELECT count(*)
            FROM   fornecedores_estoque f
            WHERE  f.estabelecimento_id = @EstabelecimentoId
              AND  (@Busca::text IS NULL
                    OR f.razao_social ILIKE '%' || @Busca::text || '%'
                    OR f.nome_fantasia ILIKE '%' || @Busca::text || '%')
              AND  (@ApenasAtivos::boolean IS NULL OR f.ativo = @ApenasAtivos::boolean);

            SELECT
                f.id                    AS Id,
                f.razao_social          AS RazaoSocial,
                f.nome_fantasia         AS NomeFantasia,
                f.cnpj                  AS Cnpj,
                f.contato_nome          AS ContatoNome,
                f.contato_telefone      AS ContatoTelefone,
                f.contato_email         AS ContatoEmail,
                f.prazo_entrega_dias    AS PrazoEntregaDias,
                f.ativo                 AS Ativo,
                (SELECT count(*) FROM itens_inventario i
                    WHERE i.fornecedor_padrao_id = f.id AND i.estabelecimento_id = f.estabelecimento_id
                ) AS QuantidadeItens
            FROM fornecedores_estoque f
            WHERE f.estabelecimento_id = @EstabelecimentoId
              AND (@Busca::text IS NULL
                   OR f.razao_social ILIKE '%' || @Busca::text || '%'
                   OR f.nome_fantasia ILIKE '%' || @Busca::text || '%')
              AND (@ApenasAtivos::boolean IS NULL OR f.ativo = @ApenasAtivos::boolean)
            ORDER BY f.ativo DESC, lower(f.razao_social)
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            ApenasAtivos = apenasAtivos,
            Tamanho = tamanho,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<FornecedorEstoqueDto>();
        return new PaginaFornecedoresEstoqueDto { Itens = itens.ToList(), Total = total, Pagina = pagina, TamanhoPagina = tamanho };
    }

    // ───────────────────────── Locais ─────────────────────────
    public async Task<PaginaLocaisEstoqueDto> ListarLocais(
        long estabelecimentoId, string? busca, bool? apenasAtivos, int pagina, int tamanho)
    {
        ValidarPagina(pagina, tamanho);
        var offset = (pagina - 1) * tamanho;

        await using var conn = new NpgsqlConnection(_connStr);

        const string sql = """
            SELECT count(*)
            FROM   locais_estoque l
            WHERE  l.estabelecimento_id = @EstabelecimentoId
              AND  (@Busca::text IS NULL OR l.nome ILIKE '%' || @Busca::text || '%')
              AND  (@ApenasAtivos::boolean IS NULL OR l.ativo = @ApenasAtivos::boolean);

            SELECT
                l.id                AS Id,
                l.nome              AS Nome,
                l.tipo              AS Tipo,
                l.andar_setor       AS AndarSetor,
                l.responsavel       AS Responsavel,
                l.ativo             AS Ativo,
                (SELECT count(*) FROM itens_inventario i
                    WHERE i.local_padrao_id = l.id AND i.estabelecimento_id = l.estabelecimento_id
                ) AS QuantidadeItens
            FROM locais_estoque l
            WHERE l.estabelecimento_id = @EstabelecimentoId
              AND (@Busca::text IS NULL OR l.nome ILIKE '%' || @Busca::text || '%')
              AND (@ApenasAtivos::boolean IS NULL OR l.ativo = @ApenasAtivos::boolean)
            ORDER BY l.ativo DESC, lower(l.nome)
            LIMIT  @Tamanho
            OFFSET @Offset;
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            ApenasAtivos = apenasAtivos,
            Tamanho = tamanho,
            Offset = offset
        };

        await using var multi = await conn.QueryMultipleAsync(sql, p);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<LocalEstoqueDto>();
        return new PaginaLocaisEstoqueDto { Itens = itens.ToList(), Total = total, Pagina = pagina, TamanhoPagina = tamanho };
    }

    private static void ValidarPagina(int pagina, int tamanho)
    {
        if (pagina < 1) throw new BusinessException("Página deve ser maior ou igual a 1.");
        if (tamanho < 1 || tamanho > 100)
            throw new BusinessException("Tamanho da página deve estar entre 1 e 100.");
    }

    // ───────────────────────── Opções (dropdowns) ─────────────────────────
    // Retornam apenas { id, nome } dos registros ATIVOS, ordenados por nome,
    // limitados a 500 — servem só pra popular selects de formulário. Filtro
    // multi-tenant é mandatório (WHERE estabelecimento_id) e o LIMIT está
    // hardcoded pra evitar consultas abusivas via query string.
    private const int LimiteOpcoes = 500;

    public async Task<IReadOnlyList<OpcaoCadastroEstoqueDto>> ObterOpcoesCategorias(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT c.id AS Id, c.nome AS Nome
            FROM   categorias_estoque c
            WHERE  c.estabelecimento_id = @EstabelecimentoId
              AND  c.ativo = TRUE
            ORDER BY lower(c.nome)
            LIMIT  @Limite;
            """;
        var rows = await conn.QueryAsync<OpcaoCadastroEstoqueDto>(sql,
            new { EstabelecimentoId = estabelecimentoId, Limite = LimiteOpcoes });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<OpcaoCadastroEstoqueDto>> ObterOpcoesFabricantes(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT f.id AS Id, f.nome AS Nome
            FROM   fabricantes_estoque f
            WHERE  f.estabelecimento_id = @EstabelecimentoId
              AND  f.ativo = TRUE
            ORDER BY lower(f.nome)
            LIMIT  @Limite;
            """;
        var rows = await conn.QueryAsync<OpcaoCadastroEstoqueDto>(sql,
            new { EstabelecimentoId = estabelecimentoId, Limite = LimiteOpcoes });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<OpcaoCadastroEstoqueDto>> ObterOpcoesFornecedores(long estabelecimentoId)
    {
        // Para fornecedores, o "rótulo" exibido no dropdown é a razão social
        // (a tela já consome esse campo nos selects).
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT f.id AS Id, f.razao_social AS Nome
            FROM   fornecedores_estoque f
            WHERE  f.estabelecimento_id = @EstabelecimentoId
              AND  f.ativo = TRUE
            ORDER BY lower(f.razao_social)
            LIMIT  @Limite;
            """;
        var rows = await conn.QueryAsync<OpcaoCadastroEstoqueDto>(sql,
            new { EstabelecimentoId = estabelecimentoId, Limite = LimiteOpcoes });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<OpcaoCadastroEstoqueDto>> ObterOpcoesLocais(long estabelecimentoId)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        const string sql = """
            SELECT l.id AS Id, l.nome AS Nome
            FROM   locais_estoque l
            WHERE  l.estabelecimento_id = @EstabelecimentoId
              AND  l.ativo = TRUE
            ORDER BY lower(l.nome)
            LIMIT  @Limite;
            """;
        var rows = await conn.QueryAsync<OpcaoCadastroEstoqueDto>(sql,
            new { EstabelecimentoId = estabelecimentoId, Limite = LimiteOpcoes });
        return rows.ToList();
    }
}
