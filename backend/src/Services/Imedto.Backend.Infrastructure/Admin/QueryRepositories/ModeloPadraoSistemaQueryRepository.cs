using Dapper;
using Npgsql;
using Imedto.Backend.Infrastructure.Database;

namespace Imedto.Backend.Infrastructure.Admin.QueryRepositories;

/// <summary>
/// Queries Dapper somente leitura para modelos de prontuário padrão-sistema (admin global).
/// Filtra sempre por eh_padrao_sistema=true — sem tenant, sem dados de outro escopo.
/// </summary>
public class ModeloPadraoSistemaQueryRepository
{
    private readonly string _connectionString;

    public ModeloPadraoSistemaQueryRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<(IEnumerable<ModeloPadraoSistemaListaItemDto> Itens, int Total)> ListarAsync(
        bool incluirInativos,
        string? busca,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    descricao       AS Descricao,
                    ativo           AS Ativo,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.modelo_de_prontuario
            WHERE   eh_padrao_sistema = true
              AND   (@IncluirInativos = true OR ativo = true)
              AND   (@Busca IS NULL OR nome ILIKE '%' || @Busca || '%')
            ORDER BY nome
            LIMIT   @Tamanho OFFSET @Offset;

            SELECT COUNT(*)
            FROM   public.modelo_de_prontuario
            WHERE  eh_padrao_sistema = true
              AND  (@IncluirInativos = true OR ativo = true)
              AND  (@Busca IS NULL OR nome ILIKE '%' || @Busca || '%');
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            IncluirInativos = incluirInativos,
            Busca = string.IsNullOrWhiteSpace(busca) ? null : busca,
            Tamanho = tamanhoPagina,
            Offset = offset
        });

        var itens = await multi.ReadAsync<ModeloPadraoSistemaListaItemDto>();
        var total = await multi.ReadSingleAsync<int>();
        return (itens, total);
    }

    public async Task<ModeloPadraoSistemaDetalheDto?> ObterAsync(long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT  id              AS Id,
                    nome            AS Nome,
                    descricao       AS Descricao,
                    estrutura       AS EstruturaJson,
                    ativo           AS Ativo,
                    criado_em       AS CriadoEm,
                    atualizado_em   AS AtualizadoEm
            FROM    public.modelo_de_prontuario
            WHERE   id = @Id
              AND   eh_padrao_sistema = true
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleOrDefaultAsync<ModeloPadraoSistemaDetalheDto>(sql, new { Id = id });
    }

    public async Task<bool> ExisteNomeParaSistema(string nome, long ignorarId = 0, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM public.modelo_de_prontuario
                WHERE  eh_padrao_sistema = true
                  AND  LOWER(nome) = LOWER(@Nome)
                  AND  id != @IgnorarId
            )
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<bool>(sql, new { Nome = nome.Trim(), IgnorarId = ignorarId });
    }
}

public class ModeloPadraoSistemaListaItemDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public bool Ativo { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? AtualizadoEm { get; init; }
}

public class ModeloPadraoSistemaDetalheDto
{
    public long Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public string EstruturaJson { get; init; } = string.Empty;
    public bool Ativo { get; init; }
    public DateTime CriadoEm { get; init; }
    public DateTime? AtualizadoEm { get; init; }
}
