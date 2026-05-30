using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Imedto.Backend.Domain.Admin;

namespace Imedto.Backend.Infrastructure.Admin;

/// <summary>
/// Implementação singleton de <see cref="IConfigGlobalReader"/>.
/// Cache em memória com TTL de 60 segundos por chave.
/// Lê de <c>imedto_config</c> via Dapper (read path).
///
/// Singleton: IMemoryCache e a connection string são singleton-safe.
/// </summary>
public class ConfigGlobalReader : IConfigGlobalReader
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private const string PrefixoCache = "imedto_config:";

    private readonly string _connStr;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigGlobalReader> _logger;

    public ConfigGlobalReader(
        AppReadConnectionString connStr,
        IMemoryCache cache,
        ILogger<ConfigGlobalReader> logger)
    {
        _connStr = connStr.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> LerInt(string chave, int defaultValue, CancellationToken ct = default)
    {
        var raw = await LerRaw(chave, ct);
        if (raw is null) return defaultValue;

        // Valor armazenado como JSON — pode ser "14" ou 14.
        var limpo = raw.Trim().Trim('"');
        return int.TryParse(limpo, out var v) ? v : defaultValue;
    }

    public async Task<string> LerString(string chave, string defaultValue, CancellationToken ct = default)
    {
        var raw = await LerRaw(chave, ct);
        if (raw is null) return defaultValue;
        return raw.Trim().Trim('"');
    }

    public async Task<bool> LerBool(string chave, bool defaultValue, CancellationToken ct = default)
    {
        var raw = await LerRaw(chave, ct);
        if (raw is null) return defaultValue;
        var limpo = raw.Trim().Trim('"').ToLowerInvariant();
        return limpo is "true" or "1" or "yes" ? true
             : limpo is "false" or "0" or "no" ? false
             : defaultValue;
    }

    public void InvalidarCache(string chave)
        => _cache.Remove(PrefixoCache + chave);

    private async Task<string?> LerRaw(string chave, CancellationToken ct)
    {
        var cacheKey = PrefixoCache + chave;

        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(_connStr);
            const string sql = "SELECT valor FROM imedto_config WHERE chave = @chave LIMIT 1";
            var valor = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(sql, new { chave }, cancellationToken: ct));

            _cache.Set(cacheKey, valor, Ttl);
            return valor;
        }
        catch (Exception ex)
        {
            // Falha ao ler config não deve interromper o fluxo; retorna null → defaultValue.
            _logger.LogError(ex, "Falha ao ler configuração global. Chave={Chave}", chave);
            return null;
        }
    }
}
