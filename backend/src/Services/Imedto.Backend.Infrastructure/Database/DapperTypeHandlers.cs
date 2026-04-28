using System.Data;
using System.Text.Json;
using Dapper;

namespace Imedto.Backend.Infrastructure.Database;

/// <summary>
/// Type handlers globais do Dapper para reconciliar tipos do Npgsql com os DTOs da aplicação.
/// Em Postgres, a coluna <c>date</c> chega como <see cref="DateOnly"/>; a <c>time</c> como
/// <see cref="TimeOnly"/>. Como os DTOs hoje usam <see cref="DateTime"/> por consistência
/// com JSON/Javascript (ISO 8601), convertemos aqui — ponto único.
/// </summary>
public static class DapperTypeHandlers
{
    private static bool _registrado;

    public static void Registrar()
    {
        if (_registrado) return;
        _registrado = true;

        SqlMapper.AddTypeHandler(new DateOnlyToNullableDateTimeHandler());
        SqlMapper.AddTypeHandler(new DateOnlyToDateTimeHandler());
        SqlMapper.AddTypeHandler(new JsonElementHandler());
    }

    /// <summary>
    /// Converte <c>jsonb</c> (string do Npgsql) em <see cref="JsonElement"/>.
    /// Sem isso, DTOs com JSONB falham no cast Dapper.
    /// </summary>
    private class JsonElementHandler : SqlMapper.TypeHandler<JsonElement>
    {
        public override JsonElement Parse(object value)
        {
            if (value is null || value is DBNull)
                return default;

            var json = value as string ?? value.ToString();
            using var doc = JsonDocument.Parse(json!);
            return doc.RootElement.Clone();
        }

        public override void SetValue(IDbDataParameter parameter, JsonElement value)
        {
            parameter.Value = value.ValueKind == JsonValueKind.Undefined ? DBNull.Value : (object)value.GetRawText();
        }
    }

    private class DateOnlyToNullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override DateTime? Parse(object value) => value switch
        {
            null => null,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            DateTime dt => dt,
            _ => Convert.ToDateTime(value)
        };

        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.Value = value.HasValue ? value.Value : DBNull.Value;
        }
    }

    private class DateOnlyToDateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) => value switch
        {
            DateOnly d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            DateTime dt => dt,
            _ => Convert.ToDateTime(value)
        };

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value;
        }
    }
}
