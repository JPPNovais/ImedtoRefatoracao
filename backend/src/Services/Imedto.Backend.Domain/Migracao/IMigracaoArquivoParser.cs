namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Porta de parsing de arquivo de migração.
/// Extrai cabeçalhos + linhas brutas de um stream CSV/JSON.
/// Implementações concretas vivem na Infrastructure.
/// </summary>
public interface IMigracaoArquivoParser
{
    bool SuportaFormato(string extensao);

    Task<ArquivoParseado> ParsearAsync(Stream stream, string nomeArquivo, CancellationToken ct = default);
}

/// <summary>
/// Resultado do parsing: cabeçalhos + linhas brutas (PII não mascarada — mascarar antes de enviar à IA).
/// </summary>
public sealed class ArquivoParseado
{
    public required string[] Cabecalhos { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, string>> Linhas { get; init; }

    /// <summary>
    /// Addendum 4 (CA70–CA72) — Blocos-candidatos decompostos do dump aninhado.
    ///
    /// Para dumps JSON com objeto raiz (múltiplas propriedades-array):
    ///   - Cada propriedade-array de objetos vira 1 bloco candidato.
    ///   - Propriedades com objeto único (config) viram BlocoCandidato.EhConfig = true.
    ///
    /// Para arquivos tabulares (CSV) e JSON-array na raiz:
    ///   - Contém exatamente 1 bloco (nome = nome do arquivo sem extensão).
    ///
    /// Sempre não-nulo — substitui o uso direto de Cabecalhos/Linhas no handler de inferência.
    /// Cabecalhos/Linhas mantidos por compatibilidade com parsers e testes existentes.
    /// </summary>
    public required IReadOnlyList<BlocoCandidato> Blocos { get; init; }
}

/// <summary>
/// Bloco candidato a migração — um array de objetos extraído de um dump aninhado
/// (ou o arquivo inteiro em caso tabular/JSON-array).
/// Addendum 4, R-S1, R-S2.
/// </summary>
public sealed class BlocoCandidato
{
    /// <summary>
    /// Nome da propriedade no objeto raiz do dump (ex.: "pacientes", "agendamentos").
    /// Para arquivos tabulares, é o nome do arquivo sem extensão.
    /// </summary>
    public required string NomeBloco { get; init; }

    /// <summary>
    /// Cabeçalhos dos campos planos deste bloco (campos de sub-objeto/array excluídos).
    /// </summary>
    public required string[] Cabecalhos { get; init; }

    /// <summary>
    /// Linhas brutas (PII não mascarada — mascarar antes de enviar à IA).
    /// Campos com sub-objeto/array não são incluídos aqui (ficam em PayloadBruto).
    /// </summary>
    public required IReadOnlyList<IReadOnlyDictionary<string, string>> Linhas { get; init; }

    /// <summary>
    /// Quando true: esta propriedade é um objeto único de config (ex.: estabelecimento{}).
    /// Não é lista de registros — não migrável. Sinalizada ao operador (D-S6).
    /// </summary>
    public bool EhConfig { get; init; }

    /// <summary>
    /// Addendum 4, Bloco D (CA81) — True quando algum valor ficou ambíguo para correção
    /// de encoding e não foi alterado — sinalizar ao operador (D-E1).
    /// </summary>
    public bool EncodingSuspeito { get; init; }

    /// <summary>
    /// Hint do nome (para o arquivo tabular, é o nome sem extensão; para dump, é a chave).
    /// Passado à IA como contexto adicional, não como decisão.
    /// </summary>
    public string HintNome => NomeBloco;
}
