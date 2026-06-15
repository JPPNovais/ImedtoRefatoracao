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
}
