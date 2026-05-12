using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imedto.Backend.SharedKernel.Json;

/// <summary>
/// Converter pra propriedades que representam <b>data civil</b> (sem hora/timezone) —
/// nascimento, vencimento, etc. — mas que ainda são <see cref="DateTime"/> no domínio
/// (não foram migradas pra <c>DateOnly</c>).
///
/// Comportamento:
/// <list type="bullet">
///   <item><b>Serialize</b>: emite só <c>"yyyy-MM-dd"</c> (sem T00:00:00). O front
///       não precisa parsear/timezone-shift.</item>
///   <item><b>Deserialize</b>: aceita <c>"yyyy-MM-dd"</c> e ISO 8601 completo
///       (legado). Em ambos, devolve <c>DateTime</c> com <c>Kind=Utc</c> à meia-noite
///       — compatível com o <c>UtcDateTimeJsonConverter</c> global.</item>
/// </list>
///
/// Sobrescreve o converter global de <see cref="DateTime"/> só quando aplicado via
/// <c>[JsonConverter(typeof(DateOnlyAsYmdJsonConverter))]</c> na propriedade.
/// </summary>
public sealed class DateOnlyAsYmdJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);

        throw new JsonException($"Data inválida: '{s}'. Use formato 'yyyy-MM-dd'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
