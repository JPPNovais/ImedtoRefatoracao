namespace Imedto.Backend.SharedKernel.Time;

/// <summary>
/// Hora de Brasília (America/Sao_Paulo) como fonte de verdade do tempo "local" da aplicação.
///
/// Use SEMPRE que precisar comparar/exibir hora humana (ex: "slot é passado?",
/// "lembrete pra hoje?"). Nunca <see cref="DateTime.Now"/> nem
/// <see cref="DateTime.ToLocalTime"/> — os dois dependem do TZ do container e
/// quebram silenciosamente se algum runtime/host estiver em UTC.
///
/// Banco continua armazenando UTC (timestamptz). Conversão pra Brasília é
/// responsabilidade exclusiva desta classe na fronteira de leitura/comparação.
/// </summary>
public static class BrasiliaTime
{
    // ID IANA cross-platform (.NET 6+ resolve em Linux/macOS/Windows desde que
    // tzdata esteja instalado — confira backend/Dockerfile linha "apk add tzdata").
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>Agora em Brasília. Kind=Unspecified pra evitar confusão com Local do servidor.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    /// <summary>Hoje em Brasília.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>
    /// Converte um DateTime para horário de Brasília.
    /// - Kind=Utc: convertido direto.
    /// - Kind=Local: convertido via UTC (preserva o instante absoluto).
    /// - Kind=Unspecified: tratado como UTC (banco timestamptz é lido pelo Npgsql como Utc;
    ///   colunas timestamp puro raras viriam Unspecified — nesses casos o assume-UTC é o
    ///   comportamento mais defensivo).
    /// </summary>
    public static DateTime ToBrasilia(this DateTime dt)
    {
        var utc = dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    /// <summary>Converte DateTimeOffset (sempre absoluto) para horário de Brasília.</summary>
    public static DateTime ToBrasilia(this DateTimeOffset dto)
        => TimeZoneInfo.ConvertTime(dto, Zone).DateTime;
}
