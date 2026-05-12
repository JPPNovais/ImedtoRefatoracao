using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imedto.Backend.API.Json;

/// <summary>
/// Força que todo <see cref="DateTime"/> recebido em DTOs seja interpretado como UTC.
/// Sem isso, ISO strings sem timezone (ex: "2026-05-26") chegam com
/// <see cref="DateTimeKind.Unspecified"/>, e o Npgsql rejeita ao gravar em colunas
/// <c>timestamp with time zone</c> com:
/// <c>"Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported."</c>
///
/// Política: tratamos todas as datas vindas do front como UTC (o front trabalha em UTC
/// e converte para apresentação local na view). Datas locais devem ser convertidas
/// para UTC pelo cliente antes de enviar.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Serializa em formato ISO 8601 UTC ("...Z"). Garante consistência com o que o
        // front recebe — sem ambiguidade de timezone do servidor.
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
    }
}

/// <summary>Versão para <c>DateTime?</c> — delega ao não-nullable se houver valor.</summary>
public sealed class UtcNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        var v = value.Value;
        var utc = v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
    }
}
