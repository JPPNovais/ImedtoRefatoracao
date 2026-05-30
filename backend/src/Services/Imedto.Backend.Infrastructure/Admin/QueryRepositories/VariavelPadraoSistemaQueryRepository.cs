using Dapper;
using Npgsql;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Queries Dapper somente leitura para variáveis pool padrão-sistema (admin global).
/// Filtra sempre por eh_padrao_sistema=true.
/// </summary>
public class VariavelPadraoSistemaQueryRepository
{
    private readonly string _connectionString;

    public VariavelPadraoSistemaQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<(IEnumerable<VariavelPadraoSistemaListaItemDto> Itens, int Total)> ListarAsync(
        bool incluirInativos,
        string? busca,
        string? categoria,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    tipo            AS Tipo,
                    ativo           AS Ativo,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.prontuario_variaveis_pool
            WHERE   eh_padrao_sistema = true
              AND   (@IncluirInativos = true OR ativo = true)
              AND   (@Busca IS NULL OR nome ILIKE '%' || @Busca || '%')
              AND   (@Categoria IS NULL OR tipo = @Categoria)
            ORDER BY tipo, nome
            LIMIT   @Tamanho OFFSET @Offset;

            SELECT COUNT(*)
            FROM   public.prontuario_variaveis_pool
            WHERE  eh_padrao_sistema = true
              AND  (@IncluirInativos = true OR ativo = true)
              AND  (@Busca IS NULL OR nome ILIKE '%' || @Busca || '%')
              AND  (@Categoria IS NULL OR tipo = @Categoria);
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            IncluirInativos = incluirInativos,
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca,
            Categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria,
            Tamanho = tamanhoPagina,
            Offset = offset
        });

        var itens = await multi.ReadAsync<VariavelPadraoSistemaListaItemDto>();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    public async Task<VariavelPadraoSistemaDetalheDto?> ObterAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    tipo            AS Tipo,
                    ativo           AS Ativo,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.prontuario_variaveis_pool
            WHERE   id = @Id
              AND   eh_padrao_sistema = true
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<VariavelPadraoSistemaDetalheDto>(sql, new { Id = id });
    }

    public async Task<bool> ExisteNomePorCategoriaParaSistema(
        string nome, string tipo, long ignorarId = 0, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM public.prontuario_variaveis_pool
                WHERE  eh_padrao_sistema = true
                  AND  tipo = @Tipo
                  AND  LOWER(nome) = LOWER(@Nome)
                  AND  id != @IgnorarId
            )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new { Nome = nome.Trim(), Tipo = tipo, IgnorarId = ignorarId });
    }
}

public class VariavelPadraoSistemaListaItemDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? AtualizadoEm { get; init; }
}

public class VariavelPadraoSistemaDetalheDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? AtualizadoEm { get; init; }
}
