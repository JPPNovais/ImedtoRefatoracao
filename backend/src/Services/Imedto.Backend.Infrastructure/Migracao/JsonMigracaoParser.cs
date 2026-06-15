using System.Text.Json;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Parser JSON — usa System.Text.Json.
/// Suporta:
///   - Array de objetos: [{...}, {...}]
///   - Objeto com propriedade array: {"data": [...]} ou {"items": [...]} etc.
/// </summary>
public sealed class JsonMigracaoParser : IMigracaoArquivoParser
{
    public bool SuportaFormato(string extensao) =>
        extensao.Equals(".json", StringComparison.OrdinalIgnoreCase);

    public async Task<ArquivoParseado> ParsearAsync(
        Stream stream,
        string nomeArquivo,
        CancellationToken ct = default)
    {
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        // Encontrar o array de objetos.
        JsonElement array;
        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            // Procura primeira propriedade que seja um array.
            array = EncontrarPrimeiroArray(root);
        }
        else
        {
            return new ArquivoParseado { Cabecalhos = [], Linhas = [] };
        }

        var linhas = new List<IReadOnlyDictionary<string, string>>();
        var cabecalhos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;

            var dict = new Dictionary<string, string>();
            foreach (var prop in item.EnumerateObject())
            {
                var valor = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    _                    => prop.Value.GetRawText(),
                };
                dict[prop.Name] = valor;
                cabecalhos.Add(prop.Name);
            }
            linhas.Add(dict);
        }

        return new ArquivoParseado
        {
            Cabecalhos = [.. cabecalhos],
            Linhas = linhas,
        };
    }

    private static JsonElement EncontrarPrimeiroArray(JsonElement obj)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
                return prop.Value;
        }
        // Nenhum array encontrado — retorna array vazio.
        return JsonDocument.Parse("[]").RootElement;
    }
}
