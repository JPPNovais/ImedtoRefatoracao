using Dapper;
using Imedto.Backend.Contracts.Termos.Dtos;
using Npgsql;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Read-side de modelos de termo (Dapper). Sempre exige <c>estabelecimento_id</c>
/// no filtro — exceto <see cref="ListarPadroes"/>, que devolve apenas padrões do
/// sistema (estabelecimento_id IS NULL) e é chamado por endpoint autenticado já
/// dentro do tenant.
/// </summary>
public interface ITermoModeloQueryRepository
{
    Task<PaginaModelosTermoDto> Listar(
        long estabelecimentoId,
        string busca,
        string categoria,
        bool somenteAtivos,
        bool incluirPadroes,
        int pagina,
        int tamanho);

    Task<IReadOnlyList<TermoModeloDto>> ListarPadroes();
    Task<TermoModeloDto> ObterPorIdDoEstabelecimentoOuPadrao(long id, long estabelecimentoId);
}

public sealed class TermoModeloQueryRepository : ITermoModeloQueryRepository
{
    private readonly string _connStr;

    public TermoModeloQueryRepository(AppReadConnectionString conn) => _connStr = conn.Value;

    public async Task<PaginaModelosTermoDto> Listar(
        long estabelecimentoId,
        string busca,
        string categoria,
        bool somenteAtivos,
        bool incluirPadroes,
        int pagina,
        int tamanho)
    {
        if (pagina < 1) pagina = 1;
        if (tamanho < 1) tamanho = 10;
        if (tamanho > 100) tamanho = 100;

        const string sqlBase = """
            FROM    public.termo_modelo m
            WHERE   m.deletado_em IS NULL
              AND   (m.estabelecimento_id = @EstabelecimentoId
                     OR (@IncluirPadroes::boolean AND m.estabelecimento_id IS NULL))
              AND   (@SomenteAtivos::boolean = FALSE OR m.ativo = TRUE)
              AND   (@Categoria::text IS NULL OR m.categoria = @Categoria)
              AND   (@Busca::text IS NULL OR lower(m.titulo) ILIKE '%' || lower(@Busca) || '%')
            """;

        var p = new
        {
            EstabelecimentoId = estabelecimentoId,
            IncluirPadroes = incluirPadroes,
            SomenteAtivos = somenteAtivos,
            Categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim(),
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim(),
            Offset = (pagina - 1) * tamanho,
            Limit = tamanho,
        };

        await using var conn = new NpgsqlConnection(_connStr);

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) {sqlBase}", p);

        var sql = $"""
            SELECT  m.id                AS Id,
                    m.estabelecimento_id AS EstabelecimentoId,
                    m.categoria         AS Categoria,
                    m.titulo            AS Titulo,
                    m.conteudo_html     AS ConteudoHtml,
                    m.ativo             AS Ativo,
                    m.versao_atual      AS VersaoAtual,
                    m.padrao_clonado_de AS PadraoClonadoDeId,
                    (m.estabelecimento_id IS NULL) AS EhPadraoDoSistema,
                    m.criado_em         AS CriadoEm,
                    m.atualizado_em     AS AtualizadoEm
            {sqlBase}
            -- Padrões do sistema descem pro fim quando há modelos do tenant.
            ORDER BY (m.estabelecimento_id IS NULL), m.titulo
            OFFSET @Offset LIMIT @Limit
            """;

        var itens = (await conn.QueryAsync<TermoModeloDto>(sql, p)).ToList();

        return new PaginaModelosTermoDto
        {
            Itens = itens,
            Pagina = pagina,
            Tamanho = tamanho,
            Total = total,
        };
    }

    public async Task<IReadOnlyList<TermoModeloDto>> ListarPadroes()
    {
        const string sql = """
            SELECT  m.id                AS Id,
                    m.estabelecimento_id AS EstabelecimentoId,
                    m.categoria         AS Categoria,
                    m.titulo            AS Titulo,
                    m.conteudo_html     AS ConteudoHtml,
                    m.ativo             AS Ativo,
                    m.versao_atual      AS VersaoAtual,
                    m.padrao_clonado_de AS PadraoClonadoDeId,
                    TRUE                AS EhPadraoDoSistema,
                    m.criado_em         AS CriadoEm,
                    m.atualizado_em     AS AtualizadoEm
            FROM    public.termo_modelo m
            WHERE   m.estabelecimento_id IS NULL
              AND   m.ativo = TRUE
              AND   m.deletado_em IS NULL
            ORDER BY m.categoria, m.titulo
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return (await conn.QueryAsync<TermoModeloDto>(sql)).ToList();
    }

    public async Task<TermoModeloDto> ObterPorIdDoEstabelecimentoOuPadrao(long id, long estabelecimentoId)
    {
        const string sql = """
            SELECT  m.id                AS Id,
                    m.estabelecimento_id AS EstabelecimentoId,
                    m.categoria         AS Categoria,
                    m.titulo            AS Titulo,
                    m.conteudo_html     AS ConteudoHtml,
                    m.ativo             AS Ativo,
                    m.versao_atual      AS VersaoAtual,
                    m.padrao_clonado_de AS PadraoClonadoDeId,
                    (m.estabelecimento_id IS NULL) AS EhPadraoDoSistema,
                    m.criado_em         AS CriadoEm,
                    m.atualizado_em     AS AtualizadoEm
            FROM    public.termo_modelo m
            WHERE   m.id = @Id
              AND   m.deletado_em IS NULL
              AND   (m.estabelecimento_id = @EstabelecimentoId OR m.estabelecimento_id IS NULL)
            LIMIT 1
            """;

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<TermoModeloDto>(sql, new { Id = id, EstabelecimentoId = estabelecimentoId });
    }
}
