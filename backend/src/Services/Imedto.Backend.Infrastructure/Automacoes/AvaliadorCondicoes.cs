using System.Text.Json;

namespace Imedto.Backend.Infrastructure.Automacoes;

/// <summary>
/// Avaliador de DSL JSON para condições de regras de automação. Formato fechado:
/// <code>
/// [
///   { "campo": "tipoServico", "operador": "==", "valor": "consulta" },
///   { "campo": "valorTotal",  "operador": ">",  "valor": 100 }
/// ]
/// </code>
/// Múltiplas condições são combinadas com AND. Uma regra sem condições (array vazio
/// ou JSON malformado) sempre dispara — convenção V1: <c>true</c> é o "fallback seguro"
/// porque o caso de uso típico é "qualquer agendamento criado", e exigir o usuário
/// digitar JSON correto antes de qualquer regra rodar seria atrito sem ganho.
///
/// Operadores suportados: <c>==</c>, <c>!=</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>,
/// <c>&gt;=</c>, <c>contains</c>, <c>startsWith</c>.
/// Comparações numéricas tentam <c>decimal</c> primeiro (financeiro), caem para string.
/// Datas: comparadas como string ISO 8601 (UTC). Comparação string-aware é case-sensitive.
/// </summary>
public static class AvaliadorCondicoes
{
    /// <summary>
    /// Avalia o array de condições contra o payload do evento. Sempre retorna bool —
    /// nunca lança: erros viram <c>false</c> (regra não dispara) para evitar que JSON
    /// malformado de um usuário crie loops de exception no worker.
    /// </summary>
    /// <remarks>Caso especial: <paramref name="condicoesJson"/> vazio/null/<c>[]</c> → <c>true</c>.</remarks>
    public static bool Avaliar(string? condicoesJson, JsonDocument payload)
    {
        if (string.IsNullOrWhiteSpace(condicoesJson) || condicoesJson.Trim() == "[]")
            return true;

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(condicoesJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return true; // V1: condicoes não-array é tratado como "sem condição"

            foreach (var cond in doc.RootElement.EnumerateArray())
            {
                if (!AvaliarCondicao(cond, payload.RootElement))
                    return false; // AND: primeira false já encerra
            }
            return true;
        }
        catch (JsonException)
        {
            return false; // condições malformadas → não dispara (evita falso-positivo)
        }
        finally
        {
            doc?.Dispose();
        }
    }

    private static bool AvaliarCondicao(JsonElement cond, JsonElement payload)
    {
        if (cond.ValueKind != JsonValueKind.Object) return false;

        if (!cond.TryGetProperty("campo", out var campoEl) || campoEl.ValueKind != JsonValueKind.String)
            return false;
        if (!cond.TryGetProperty("operador", out var opEl) || opEl.ValueKind != JsonValueKind.String)
            return false;
        if (!cond.TryGetProperty("valor", out var valorEl))
            return false;

        var campo = campoEl.GetString()!;
        var operador = opEl.GetString()!;

        // Resolve "campo" no payload — case-insensitive porque domain events usam PascalCase
        // mas o usuário tipicamente escreve camelCase ("tipoServico" vs "TipoServico").
        var valorPayload = ResolverCampo(payload, campo);
        if (valorPayload is null) return false;

        return Comparar(valorPayload.Value, operador, valorEl);
    }

    private static JsonElement? ResolverCampo(JsonElement obj, string campo)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;

        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, campo, StringComparison.OrdinalIgnoreCase))
                return prop.Value;
        }
        return null;
    }

    private static bool Comparar(JsonElement payloadValor, string operador, JsonElement esperado)
    {
        // Tenta numérico primeiro — cobre valores financeiros, contagens, IDs.
        if (TentarNumerico(payloadValor, esperado, out var nA, out var nB))
        {
            return operador switch
            {
                "==" => nA == nB,
                "!=" => nA != nB,
                "<"  => nA <  nB,
                ">"  => nA >  nB,
                "<=" => nA <= nB,
                ">=" => nA >= nB,
                _    => false
            };
        }

        // Booleano direto (status flags, ativa/inativa).
        if (payloadValor.ValueKind == JsonValueKind.True || payloadValor.ValueKind == JsonValueKind.False)
        {
            var bA = payloadValor.GetBoolean();
            var bB = esperado.ValueKind == JsonValueKind.True
                || (esperado.ValueKind == JsonValueKind.String
                    && bool.TryParse(esperado.GetString(), out var p) && p);
            return operador switch
            {
                "==" => bA == bB,
                "!=" => bA != bB,
                _    => false
            };
        }

        // Fallback string (case-sensitive — datas ISO comparam corretamente, ex: "2026-04-28").
        var sA = ValorComoString(payloadValor);
        var sB = ValorComoString(esperado);
        if (sA is null || sB is null) return false;

        return operador switch
        {
            "==" => sA == sB,
            "!=" => sA != sB,
            "<"  => string.CompareOrdinal(sA, sB) <  0,
            ">"  => string.CompareOrdinal(sA, sB) >  0,
            "<=" => string.CompareOrdinal(sA, sB) <= 0,
            ">=" => string.CompareOrdinal(sA, sB) >= 0,
            "contains"   => sA.Contains(sB, StringComparison.OrdinalIgnoreCase),
            "startsWith" => sA.StartsWith(sB, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool TentarNumerico(JsonElement a, JsonElement b, out decimal nA, out decimal nB)
    {
        nA = 0; nB = 0;
        var okA = a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out nA);
        var okB = b.ValueKind == JsonValueKind.Number && b.TryGetDecimal(out nB);
        if (okA && okB) return true;

        // Permite "valor": "100" (string) no JSON do usuário — comum.
        if (!okA && a.ValueKind == JsonValueKind.String && decimal.TryParse(a.GetString(), out nA)) okA = true;
        if (!okB && b.ValueKind == JsonValueKind.String && decimal.TryParse(b.GetString(), out nB)) okB = true;

        return okA && okB;
    }

    private static string? ValorComoString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.ToString(),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        JsonValueKind.Null   => null,
        _ => el.GetRawText()
    };
}
