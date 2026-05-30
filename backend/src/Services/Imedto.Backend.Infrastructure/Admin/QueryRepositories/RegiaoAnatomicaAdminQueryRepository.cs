using Dapper;
using Npgsql;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Queries Dapper somente leitura para regiões anatômicas (catálogo global por construção).
/// Monta árvore completa em memória a partir de uma única query flat.
/// </summary>
public class RegiaoAnatomicaAdminQueryRepository
{
    private readonly string _connectionString;

    public RegiaoAnatomicaAdminQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    /// <summary>
    /// Retorna árvore de regiões agrupada por vista, montada server-side.
    /// Inclui inativas quando solicitado.
    /// </summary>
    public async Task<IEnumerable<RegiaoAnatomicaNoDto>> ObterArvoreAsync(
        bool incluirInativas = false,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT  id              AS Id,
                    codigo          AS Codigo,
                    nome            AS Nome,
                    pai_codigo      AS PaiCodigo,
                    nivel           AS Nivel,
                    vista           AS Vista,
                    template_texto  AS TemplateTexto,
                    ordem           AS Ordem,
                    lateralidade    AS Lateralidade,
                    ativo           AS Ativo
            FROM    public.regioes_anatomicas_catalogo
            WHERE   @IncluirInativas = true OR ativo = true
            ORDER BY nivel, ordem, nome
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var flat = (await conn.QueryAsync<RegiaoAnatomicaFlatDto>(sql, new { IncluirInativas = incluirInativas })).ToList();

        return MontarArvore(flat);
    }

    public async Task<RegiaoAnatomicaNoDto?> ObterPorIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT  id              AS Id,
                    codigo          AS Codigo,
                    nome            AS Nome,
                    pai_codigo      AS PaiCodigo,
                    nivel           AS Nivel,
                    vista           AS Vista,
                    template_texto  AS TemplateTexto,
                    ordem           AS Ordem,
                    lateralidade    AS Lateralidade,
                    ativo           AS Ativo
            FROM    public.regioes_anatomicas_catalogo
            WHERE   id = @Id
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var item = await conn.QuerySingleOrDefaultAsync<RegiaoAnatomicaFlatDto>(sql, new { Id = id });
        if (item is null) return null;

        return new RegiaoAnatomicaNoDto
        {
            Id = item.Id,
            Codigo = item.Codigo,
            Nome = item.Nome,
            PaiCodigo = item.PaiCodigo,
            Nivel = item.Nivel,
            Vista = item.Vista,
            TemplateTexto = item.TemplateTexto,
            Ordem = item.Ordem,
            Lateralidade = item.Lateralidade,
            Ativo = item.Ativo,
            Filhos = []
        };
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, long ignorarId = 0, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM public.regioes_anatomicas_catalogo
                WHERE  LOWER(codigo) = LOWER(@Codigo) AND id != @IgnorarId
            )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new { Codigo = codigo.Trim(), IgnorarId = ignorarId });
    }

    public async Task<bool> TemFilhosAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM public.regioes_anatomicas_catalogo p
                JOIN   public.regioes_anatomicas_catalogo f ON f.pai_codigo = p.codigo
                WHERE  p.id = @Id
            )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    private static IEnumerable<RegiaoAnatomicaNoDto> MontarArvore(IEnumerable<RegiaoAnatomicaFlatDto> flat)
    {
        var todos = flat.Select(f => new RegiaoAnatomicaNoDto
        {
            Id = f.Id,
            Codigo = f.Codigo,
            Nome = f.Nome,
            PaiCodigo = f.PaiCodigo,
            Nivel = f.Nivel,
            Vista = f.Vista,
            TemplateTexto = f.TemplateTexto,
            Ordem = f.Ordem,
            Lateralidade = f.Lateralidade,
            Ativo = f.Ativo,
            Filhos = []
        }).ToList();

        var porCodigo = todos.ToDictionary(r => r.Codigo);
        var raizes = new List<RegiaoAnatomicaNoDto>();

        foreach (var no in todos.OrderBy(r => r.Ordem).ThenBy(r => r.Nome))
        {
            if (no.PaiCodigo is not null && porCodigo.TryGetValue(no.PaiCodigo, out var pai))
            {
                pai.Filhos.Add(no);
            }
            else
            {
                raizes.Add(no);
            }
        }

        return raizes.OrderBy(r => r.Vista).ThenBy(r => r.Ordem).ThenBy(r => r.Nome);
    }

    private class RegiaoAnatomicaFlatDto
    {
        public long Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string? PaiCodigo { get; init; }
        public short Nivel { get; init; }
        public string? Vista { get; init; }
        public string? TemplateTexto { get; init; }
        public short Ordem { get; init; }
        public bool Lateralidade { get; init; }
        public bool Ativo { get; init; }
    }
}

public class RegiaoAnatomicaNoDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? PaiCodigo { get; init; }
    public short Nivel { get; init; }
    public string? Vista { get; init; }
    public string? TemplateTexto { get; set; }
    public short Ordem { get; set; }
    public bool Lateralidade { get; set; }
    public bool Ativo { get; set; }
    public List<RegiaoAnatomicaNoDto> Filhos { get; set; } = [];
}
