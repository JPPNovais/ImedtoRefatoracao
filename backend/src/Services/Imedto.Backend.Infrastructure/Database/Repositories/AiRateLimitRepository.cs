using Dapper;
using Npgsql;
using Imedto.Backend.Domain.Ia;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Rate limit de chamadas IA por usuário, janela fixa de 1 minuto. Idempotente
/// via upsert atômico no índice único (usuario_id, periodo_inicio).
/// </summary>
public class AiRateLimitRepository : IAiRateLimitRepository
{
    private readonly string _connectionString;

    public AiRateLimitRepository(AppReadConnectionString connection)
    {
        _connectionString = connection.Value;
    }

    public async Task<bool> RegistrarTentativaAsync(
        Guid usuarioId,
        int limitePorMinuto,
        CancellationToken ct = default)
    {
        if (limitePorMinuto <= 0) return true;

        // Trunca o "agora" no minuto — chave da janela atual.
        var agora = DateTime.UtcNow;
        var periodoInicio = new DateTime(
            agora.Year, agora.Month, agora.Day, agora.Hour, agora.Minute, 0,
            DateTimeKind.Utc);

        // Upsert atômico: insere com contagem 1 ou incrementa se já existe.
        // RETURNING devolve a contagem efetiva — comparamos com o limite.
        const string sql = """
            INSERT INTO public.ai_rate_limits (usuario_id, periodo_inicio, contagem, ultimo_acesso)
            VALUES (@UsuarioId, @PeriodoInicio, 1, NOW())
            ON CONFLICT (usuario_id, periodo_inicio) DO UPDATE SET
                contagem      = public.ai_rate_limits.contagem + 1,
                ultimo_acesso = NOW()
            RETURNING contagem
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        var contagem = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { UsuarioId = usuarioId, PeriodoInicio = periodoInicio },
            cancellationToken: ct));

        return contagem <= limitePorMinuto;
    }
}
