using System.Text.Json;
using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Parser JSON — usa System.Text.Json.
/// Suporta:
///   - Array de objetos na raiz: [{...}, {...}] → 1 bloco-candidato (CA71 — não regride).
///   - Objeto raiz com múltiplas propriedades-array: {"pacientes":[...], "agendamentos":[...]}
///     → N blocos-candidatos, um por propriedade-array de objetos (CA70).
///   - Objeto raiz com propriedade objeto único (config): {"estabelecimento":{...}}
///     → BlocoCandidato com EhConfig = true (sinalizado, não migrável — D-S6, CA78).
///   - Sub-objetos e arrays dentro dos registros são excluídos dos Cabecalhos/Linhas
///     mas preservados no payload bruto via GetRawText (CA72/R-S2/D-S4).
///
/// Normalização de encoding (Bloco D, R-S8, CA80/CA81):
///   - Após parsing, cada valor de string passa por MojibakeNormalizador.TentarCorrigir.
///   - Se algum valor ficou suspeito (ambíguo), o bloco é marcado com EncodingSuspeito.
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

        List<BlocoCandidato> blocos;

        if (root.ValueKind == JsonValueKind.Array)
        {
            // JSON-array na raiz → 1 bloco (CA71 — compatibilidade garantida).
            var nomeBloco = Path.GetFileNameWithoutExtension(nomeArquivo);
            var bloco = ExtrairBloco(nomeBloco, root);
            blocos = [bloco];
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            // Objeto raiz → N blocos (CA70 — corrige EncontrarPrimeiroArray).
            blocos = DecomporObjetoRaiz(root);

            // Fallback: objeto raiz sem nenhuma propriedade array de objetos.
            if (blocos.Count == 0)
                return new ArquivoParseado { Cabecalhos = [], Linhas = [], Blocos = [] };
        }
        else
        {
            return new ArquivoParseado { Cabecalhos = [], Linhas = [], Blocos = [] };
        }

        // Para compatibilidade com código existente: Cabecalhos/Linhas do primeiro bloco migrável.
        var primeiroBlocoMigravel = blocos.FirstOrDefault(b => !b.EhConfig);
        return new ArquivoParseado
        {
            Cabecalhos = primeiroBlocoMigravel?.Cabecalhos ?? [],
            Linhas = primeiroBlocoMigravel?.Linhas ?? [],
            Blocos = blocos,
        };
    }

    // ── Decomposição do objeto raiz em blocos-candidatos ─────────────────────

    /// <summary>
    /// Enumera todas as propriedades do objeto raiz.
    /// - propriedade = array de objetos → BlocoCandidato normal (CA70/R-S1)
    /// - propriedade = objeto único → BlocoCandidato.EhConfig = true (D-S6)
    /// - propriedade = escalar, array de escalares, array vazio → ignorada (R-S1)
    /// </summary>
    private static List<BlocoCandidato> DecomporObjetoRaiz(JsonElement obj)
    {
        var blocos = new List<BlocoCandidato>();

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                // Verifica se é array de objetos (não array de escalares).
                var temObjetos = false;
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object) { temObjetos = true; break; }
                }

                if (temObjetos)
                {
                    blocos.Add(ExtrairBloco(prop.Name, prop.Value));
                }
                // Array vazio ou de escalares → ignora (R-S1).
            }
            else if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                // Objeto único de config — sinalizado, não migrável (D-S6/CA78).
                blocos.Add(new BlocoCandidato
                {
                    NomeBloco = prop.Name,
                    Cabecalhos = [],
                    Linhas = [],
                    EhConfig = true,
                });
            }
            // Escalares no topo → ignorados (R-S1).
        }

        return blocos;
    }

    /// <summary>
    /// Extrai um BlocoCandidato de um elemento array.
    /// Aplica normalização de encoding após extrair os valores (R-S8/CA80/CA81).
    ///
    /// Campos planos (string/número/data/booleano/null) → entram em Linhas.
    /// Campos com sub-objeto ou array → excluídos dos Cabecalhos/Linhas (R-S2/D-S4).
    ///   O valor bruto seria acessível via GetRawText() se necessário (payload_bruto).
    /// </summary>
    private static BlocoCandidato ExtrairBloco(string nomeBloco, JsonElement array)
    {
        var linhas = new List<IReadOnlyDictionary<string, string>>();
        var cabecalhos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var encodingSuspeito = false;

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;

            var dict = new Dictionary<string, string>();
            foreach (var prop in item.EnumerateObject())
            {
                // CA72/R-S2/D-S4: sub-objetos e arrays dentro de registros são excluídos
                // dos campos mapeáveis. Ficam apenas no payload bruto (não incluído aqui).
                if (prop.Value.ValueKind == JsonValueKind.Object ||
                    prop.Value.ValueKind == JsonValueKind.Array)
                {
                    // Não adiciona ao dict nem aos cabeçalhos — não mapeia sub-objeto (D-S4).
                    continue;
                }

                var valorRaw = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    _                    => prop.Value.GetRawText(),
                };

                // Normalização de encoding por valor (R-S8/CA80/CA81).
                var (valorNormalizado, suspeito) = MojibakeNormalizador.TentarCorrigir(valorRaw);
                if (suspeito) encodingSuspeito = true;

                dict[prop.Name] = valorNormalizado;
                cabecalhos.Add(prop.Name);
            }
            linhas.Add(dict);
        }

        return new BlocoCandidato
        {
            NomeBloco = nomeBloco,
            Cabecalhos = [.. cabecalhos],
            Linhas = linhas,
            EhConfig = false,
            EncodingSuspeito = encodingSuspeito,
        };
    }
}
