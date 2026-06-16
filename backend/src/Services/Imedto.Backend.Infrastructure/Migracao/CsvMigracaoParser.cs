using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Parser CSV simples — sem lib externa.
/// Suporta delimitador vírgula e ponto-e-vírgula (detecta automaticamente pelo cabeçalho).
/// </summary>
public sealed class CsvMigracaoParser : IMigracaoArquivoParser
{
    public bool SuportaFormato(string extensao) =>
        extensao.Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<ArquivoParseado> ParsearAsync(
        Stream stream,
        string nomeArquivo,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);

        // Lê cabeçalho para detectar delimitador.
        var cabecalhoLinha = await reader.ReadLineAsync(ct) ?? string.Empty;
        var delimitador = DetectarDelimitador(cabecalhoLinha);
        var cabecalhos = SplitLinha(cabecalhoLinha, delimitador);

        var linhas = new List<IReadOnlyDictionary<string, string>>();
        string? linha;
        while ((linha = await reader.ReadLineAsync(ct)) != null)
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(linha)) continue;

            var valores = SplitLinha(linha, delimitador);
            var dict = new Dictionary<string, string>(cabecalhos.Length);
            for (var i = 0; i < cabecalhos.Length; i++)
            {
                var valor = i < valores.Length ? valores[i].Trim('"') : string.Empty;
                dict[cabecalhos[i]] = valor;
            }
            linhas.Add(dict);
        }

        // Normalização de encoding nos valores do CSV (R-S8/CA80/CA81).
        var linhasNormalizadas = new List<IReadOnlyDictionary<string, string>>(linhas.Count);
        var encodingSuspeito = false;
        foreach (var linha2 in linhas)
        {
            var (linhaNorm, suspeita) = MojibakeNormalizador.NormalizarLinha(linha2);
            linhasNormalizadas.Add(linhaNorm);
            if (suspeita) encodingSuspeito = true;
        }

        // Addendum 4: CSV → 1 bloco candidato com nome = arquivo sem extensão.
        var nomeBloco = Path.GetFileNameWithoutExtension(nomeArquivo);
        var bloco = new BlocoCandidato
        {
            NomeBloco = nomeBloco,
            Cabecalhos = cabecalhos,
            Linhas = linhasNormalizadas,
            EhConfig = false,
            EncodingSuspeito = encodingSuspeito,
        };

        return new ArquivoParseado
        {
            Cabecalhos = cabecalhos,
            Linhas = linhasNormalizadas,
            Blocos = [bloco],
        };
    }

    private static char DetectarDelimitador(string linha)
    {
        var contagemPontoVirgula = linha.Count(c => c == ';');
        var contagemVirgula      = linha.Count(c => c == ',');
        return contagemPontoVirgula > contagemVirgula ? ';' : ',';
    }

    private static string[] SplitLinha(string linha, char delimitador)
    {
        // Split simples — não trata campos com aspas contendo o delimitador.
        // Suficiente para a amostragem de mapeamento (não precisa de CSV completo RFC 4180).
        return linha.Split(delimitador);
    }
}
