using System.Text.Json;
using Dapper;
using Npgsql;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Queries Dapper somente-leitura para modelos de permissão padrão do sistema (admin global).
/// Filtra sempre por estabelecimento_id IS NULL — escopo global.
/// </summary>
public class ModeloPermissaoPadraoSistemaQueryRepository
{
    private readonly string _connectionString;

    public ModeloPermissaoPadraoSistemaQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    // Construtor sem parâmetros para Moq em testes
    protected ModeloPermissaoPadraoSistemaQueryRepository() { _connectionString = string.Empty; }

    public virtual async Task<(IEnumerable<ModeloPermissaoPadraoListaItemDto> Itens, int Total)> ListarAsync(
        string? busca,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    tipo_acesso     AS TipoAcesso,
                    descricao       AS Descricao,
                    icone           AS Icone,
                    cor             AS Cor,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.modelo_permissao_estabelecimento
            WHERE   estabelecimento_id IS NULL
              AND   (@Busca IS NULL OR nome ILIKE '%' || @Busca || '%')
            ORDER BY nome
            LIMIT   @Tamanho OFFSET @Offset;

            SELECT COUNT(*)
            FROM   public.modelo_permissao_estabelecimento
            WHERE  estabelecimento_id IS NULL
              AND  (@Busca IS NULL OR nome ILIKE '%' || @Busca || '%');
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca,
            Tamanho = tamanhoPagina,
            Offset = offset,
        });

        var itens = await multi.ReadAsync<ModeloPermissaoPadraoListaItemDto>();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    public virtual async Task<ModeloPermissaoPadraoDetalheDto?> ObterAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    tipo_acesso     AS TipoAcesso,
                    permissoes::text AS PermissoesJson,
                    permissoes_extras::text AS PermissoesExtrasJson,
                    descricao       AS Descricao,
                    icone           AS Icone,
                    cor             AS Cor,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.modelo_permissao_estabelecimento
            WHERE   id = @Id
              AND   estabelecimento_id IS NULL
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<ModeloPermissaoPadraoDetalheDto>(sql, new { Id = id });
    }

    /// <summary>
    /// Retorna os ids de todos os estabelecimentos existentes para a propagação retroativa.
    /// Usado pelos handlers de criar/atualizar para materializar cópias.
    /// </summary>
    public virtual async Task<IReadOnlyList<long>> ListarIdsEstabelecimentos(CancellationToken ct = default)
    {
        const string sql = "SELECT id FROM public.estabelecimento ORDER BY id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<long>(sql);
        return ids.ToList();
    }

    /// <summary>
    /// Conta quantos estabelecimentos terão a cópia propagada.
    /// Exposto para o step de confirmação de impacto no frontend (CA — confirmação).
    /// </summary>
    public virtual async Task<int> ContarEstabelecimentos(CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(*) FROM public.estabelecimento";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql);
    }
}

public class ModeloPermissaoPadraoListaItemDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string TipoAcesso { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public string? Icone { get; init; }
    public string? Cor { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? AtualizadoEm { get; init; }
}

public class ModeloPermissaoPadraoDetalheDto : ModeloPermissaoPadraoListaItemDto
{
    public string? PermissoesJson { get; init; }
    public string? PermissoesExtrasJson { get; init; }

    public IReadOnlyList<string> Permissoes =>
        ParseJsonLista(PermissoesJson);

    public IReadOnlyList<string> PermissoesExtras =>
        ParseJsonLista(PermissoesExtrasJson);

    private static IReadOnlyList<string> ParseJsonLista(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return Array.Empty<string>(); }
    }
}
