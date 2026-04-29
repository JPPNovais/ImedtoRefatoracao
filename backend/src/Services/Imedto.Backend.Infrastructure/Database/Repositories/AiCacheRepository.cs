using Dapper;
using Npgsql;
using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Cache de outputs de IA, indexado pelo hash SHA256 do prompt. Implementação
/// Dapper para evitar tracking do EF (cada upsert é independente).
/// </summary>
public class AiCacheRepository : IAiCacheRepository
{
    private readonly string _connectionString;

    public AiCacheRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<string?> ObterAsync(string promptHash, CancellationToken ct = default)
    {
        const string sql = """
            SELECT  output
            FROM    public.ai_outputs_cache
            WHERE   prompt_hash = @PromptHash
              AND   expira_em > NOW()
            LIMIT   1
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { PromptHash = promptHash }, cancellationToken: ct));
    }

    public async Task SalvarAsync(
        string promptHash,
        long estabelecimentoId,
        string endpoint,
        string output,
        DateTime expiraEm,
        int? tokensIn = null,
        int? tokensOut = null,
        CancellationToken ct = default)
    {
        // Upsert: prompt_hash é PK. Atualiza output/expiração se já existir.
        const string sql = """
            INSERT INTO public.ai_outputs_cache
                (prompt_hash, estabelecimento_id, endpoint, output, tokens_in, tokens_out, expira_em, criado_em)
            VALUES
                (@PromptHash, @EstabelecimentoId, @Endpoint, @Output, @TokensIn, @TokensOut, @ExpiraEm, NOW())
            ON CONFLICT (prompt_hash) DO UPDATE SET
                output     = EXCLUDED.output,
                tokens_in  = EXCLUDED.tokens_in,
                tokens_out = EXCLUDED.tokens_out,
                expira_em  = EXCLUDED.expira_em,
                endpoint   = EXCLUDED.endpoint
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                PromptHash = promptHash,
                EstabelecimentoId = estabelecimentoId,
                Endpoint = endpoint,
                Output = output,
                TokensIn = tokensIn,
                TokensOut = tokensOut,
                ExpiraEm = expiraEm
            },
            cancellationToken: ct));
    }

    public async Task<int> RemoverExpiradosAsync(CancellationToken ct = default)
    {
        const string sql = "DELETE FROM public.ai_outputs_cache WHERE expira_em <= NOW()";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }
}
