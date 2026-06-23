using Dapper;
using Npgsql;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Queries Dapper somente-leitura para o catálogo global de categorias financeiras padrão (admin global).
/// Espelha <see cref="ModeloPermissaoPadraoSistemaQueryRepository"/>.
/// Briefing 2026-06-22_003 — M3.
/// </summary>
public class CategoriaFinanceiraPadraoSistemaQueryRepository
{
    private readonly string _connectionString;

    public CategoriaFinanceiraPadraoSistemaQueryRepository(AppReadConnectionString connection)
        => _connectionString = connection.Value;

    // Construtor sem parâmetros para Moq em testes
    protected CategoriaFinanceiraPadraoSistemaQueryRepository() { _connectionString = string.Empty; }

    public virtual async Task<(IEnumerable<CategoriaFinanceiraPadraoListaItemDto> Itens, int Total)> ListarAsync(
        string? tipo,
        bool? ativas,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = """
            SELECT  id          AS Id,
                    nome        AS Nome,
                    tipo        AS Tipo,
                    ativo       AS Ativo,
                    criada_em   AS CriadaEm,
                    atualizada_em AS AtualizadaEm
            FROM    public.categorias_financeiras_padrao_sistema
            WHERE   (@Tipo::text   IS NULL OR tipo   = @Tipo::text)
              AND   (@Ativas::bool IS NULL OR ativo  = @Ativas::bool)
            ORDER BY tipo, nome
            LIMIT @Tamanho OFFSET @Offset;

            SELECT COUNT(*)
            FROM   public.categorias_financeiras_padrao_sistema
            WHERE  (@Tipo::text   IS NULL OR tipo   = @Tipo::text)
              AND  (@Ativas::bool IS NULL OR ativo  = @Ativas::bool);
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo,
            Ativas = ativas,
            Tamanho = tamanhoPagina,
            Offset = offset,
        });

        var itens = await multi.ReadAsync<CategoriaFinanceiraPadraoListaItemDto>();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    /// <summary>
    /// Retorna os ids de todos os estabelecimentos existentes para propagação retroativa.
    /// Espelha <see cref="ModeloPermissaoPadraoSistemaQueryRepository.ListarIdsEstabelecimentos"/>.
    /// </summary>
    public virtual async Task<IReadOnlyList<long>> ListarIdsEstabelecimentos(CancellationToken ct = default)
    {
        const string sql = "SELECT id FROM public.estabelecimento ORDER BY id";
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<long>(sql);
        return ids.ToList();
    }

    /// <summary>
    /// Verifica se o estabelecimento já possui uma categoria com o mesmo nome
    /// (independente de tipo — reflexo do índice único de tenant por nome sem tipo).
    /// Usado para a propagação idempotente ao criar global (R3).
    /// </summary>
    public virtual async Task<IReadOnlyList<long>> ListarEstabelecimentosComNome(
        string nome,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT estabelecimento_id
            FROM   public.categorias_financeiras
            WHERE  nome = @Nome
            """;
        await using var conn = new NpgsqlConnection(_connectionString);
        var ids = await conn.QueryAsync<long>(sql, new { Nome = nome });
        return ids.ToList();
    }
}

public class CategoriaFinanceiraPadraoListaItemDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTime CriadaEm { get; init; }
    public DateTime? AtualizadaEm { get; init; }
}
