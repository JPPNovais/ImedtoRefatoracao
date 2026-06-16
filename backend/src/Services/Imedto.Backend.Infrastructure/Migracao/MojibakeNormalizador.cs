namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Normalização determinística de mojibake UTF-8↔Latin-1 (addendum 4, Bloco D — R-S8, CA80, CA81).
///
/// Caso dominante em exports PT-BR: arquivo UTF-8 lido como Latin-1/Windows-1252.
/// Ex.: "Cirurgia PlÃ¡stica" → "Cirurgia Plástica".
///
/// A detecção é feita pelo padrão característico de mojibake PT-BR:
///   Ã + byte-de-continuação: Ã¡=á, Ã©=é, Ã£=ã, Ã§=ç, Ã³=ó, etc.
/// Texto português corretamente codificado NÃO tem o caractere Ã isolado antes
/// de letras minúsculas — portanto esse padrão é diagnóstico de mojibake.
///
/// Estratégia conservadora (D-E1):
///   - Detecta padrão Ã + continuação (sinal claro de mojibake PT-BR).
///   - Faz round-trip Latin-1→UTF-8.
///   - Verifica que o resultado não tem replacement chars.
///   - Se OK → adota. Caso contrário → mantém original e sinaliza.
///
/// Nunca usa IA; nunca corrompe dado bom; sinaliza quando ambíguo.
/// </summary>
public static class MojibakeNormalizador
{
    private static readonly System.Text.Encoding Latin1 =
        System.Text.Encoding.GetEncoding("iso-8859-1");

    private static readonly System.Text.Encoding Utf8 =
        System.Text.Encoding.UTF8;

    /// <summary>
    /// Tenta corrigir mojibake de uma string.
    /// </summary>
    /// <returns>
    /// (textoCorrigido, encodingSuspeito):
    ///   - textoCorrigido: texto corrigido (ou o original se não corrigível de forma segura).
    ///   - encodingSuspeito: true apenas quando há indício de mojibake mas a correção é ambígua.
    /// </returns>
    public static (string TextoCorrigido, bool EncodingSuspeito) TentarCorrigir(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return (texto, false);

        // Passo 1: detecta padrão diagnóstico de mojibake PT-BR.
        // Se não houver sequência Ã + byte-de-continuação, o texto está correto — não alterar.
        if (!TemPadraoMojibakePtBr(texto))
            return (texto, false);

        try
        {
            // Passo 2: round-trip Latin-1 → bytes → UTF-8.
            // No mojibake, o arquivo era UTF-8 mas foi lido como Latin-1, então
            // re-codificar como Latin-1 e re-decodificar como UTF-8 recupera o original.
            var bytesLatin1 = Latin1.GetBytes(texto);
            var candidato = Utf8.GetString(bytesLatin1);

            // Passo 3: verifica que a correção não introduziu replacement chars (U+FFFD).
            // Se introduziu → a string tinha bytes inválidos para UTF-8 → sinaliza.
            if (candidato.Contains('�'))
                return (texto, true);

            // Passo 4: verifica que o candidato ainda tem padrão mojibake residual.
            // Se ainda tem Ã + continuação após o round-trip, a correção foi parcial → suspeito.
            if (TemPadraoMojibakePtBr(candidato))
                return (texto, true);

            // Correção limpa: adota o candidato.
            return (candidato, false);
        }
        catch
        {
            // Qualquer exceção na conversão → conservador: mantém original e sinaliza.
            return (texto, true);
        }
    }

    /// <summary>
    /// Aplica TentarCorrigir em todos os valores de uma linha (dicionário col→valor).
    /// Retorna (linha corrigida, se algum campo ficou suspeito).
    /// </summary>
    public static (IReadOnlyDictionary<string, string> LinhaCorrigida, bool EncodingSuspeito)
        NormalizarLinha(IReadOnlyDictionary<string, string> linha)
    {
        var dict = new Dictionary<string, string>(linha.Count, StringComparer.OrdinalIgnoreCase);
        var algumSuspeito = false;

        foreach (var (col, valor) in linha)
        {
            var (corrigido, suspeito) = TentarCorrigir(valor);
            dict[col] = corrigido;
            if (suspeito) algumSuspeito = true;
        }

        return (dict, algumSuspeito);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Detecta o padrão diagnóstico de mojibake PT-BR: caractere Ã (U+00C3) seguido de
    /// um byte de continuação UTF-8 decodificado erroneamente como Latin-1 (U+00A0–U+00BF).
    ///
    /// Exemplos: "Ã¡"=á, "Ã©"=é, "Ã£"=ã, "Ã§"=ç, "Ã³"=ó, "Ã"=Á (Ã + U+0081 fora range, cobre Ã sozinho)
    /// Texto português válido em UTF-8 NÃO contém este padrão.
    /// </summary>
    private static bool TemPadraoMojibakePtBr(string texto)
    {
        for (int i = 0; i < texto.Length - 1; i++)
        {
            var c = texto[i];
            var next = texto[i + 1];
            // U+00C3 = Ã, seguido de byte de continuação UTF-8 em Latin-1 (U+0080–U+00BF)
            if (c == 'Ã' && next >= '' && next <= '¿')
                return true;
        }
        return false;
    }
}
