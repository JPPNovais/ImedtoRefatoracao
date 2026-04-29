using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Jobs;

namespace Imedto.Backend.Infrastructure.Jobs.Handlers;

/// <summary>
/// Remove entradas expiradas da tabela <c>ai_outputs_cache</c>.
/// Mantém a tabela enxuta sem depender de TTL do Postgres.
/// </summary>
public class LimparCacheIaJob : IJobHandler
{
    public string Nome => "limpar-cache-ia";

    private readonly string _connectionString;
    private readonly ILogger<LimparCacheIaJob> _logger;

    public LimparCacheIaJob(AppReadConnectionString connection, ILogger<LimparCacheIaJob> logger)
    {
        _connectionString = connection.Value;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        const string sql = "DELETE FROM public.ai_outputs_cache WHERE expira_em < now()";

        await using var conn = new NpgsqlConnection(_connectionString);
        var removidos = await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));

        _logger.LogInformation(
            "[Job:{Nome}] Removidos {Removidos} entradas expiradas de ai_outputs_cache.",
            Nome, removidos);
    }
}
