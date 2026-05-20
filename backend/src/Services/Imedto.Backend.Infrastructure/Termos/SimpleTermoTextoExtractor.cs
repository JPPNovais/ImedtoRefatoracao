using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Imedto.Backend.Domain.Termos;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Extrai uma versão texto puro do HTML para snapshot/busca. Não é um parser
/// completo — apenas o suficiente para preservar quebras entre blocos, listas
/// e decodificar entidades HTML. Implementação singleton e stateless.
///
/// Não usa AngleSharp/HtmlAgilityPack — o conteúdo entrante já passou pelo
/// <see cref="ITermoHtmlSanitizer"/>, então é HTML conhecido e limitado.
/// </summary>
public sealed class SimpleTermoTextoExtractor : ITermoTextoExtractor
{
    private static readonly Regex TagAbreLista = new(@"<li\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagBloco = new(@"</(p|div|h[1-6]|li|ul|ol|blockquote|hr)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagBr = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagQualquer = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex EspacosExcessivos = new(@"[ \t]{2,}", RegexOptions.Compiled);
    private static readonly Regex QuebrasExcessivas = new(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex AnchorComHref = new(
        @"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>(.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    public string Extrair(string conteudoHtml)
    {
        if (string.IsNullOrWhiteSpace(conteudoHtml)) return string.Empty;

        var s = conteudoHtml;

        // Links viram "texto (url)" — preserva semântica.
        s = AnchorComHref.Replace(s, m => $"{m.Groups[2].Value} ({m.Groups[1].Value})");

        // <br> vira quebra de linha simples.
        s = TagBr.Replace(s, "\n");

        // Itens de lista ficam com prefixo "- ".
        s = TagAbreLista.Replace(s, "- ");

        // Fecha de bloco vira quebra dupla (separação de parágrafos).
        s = TagBloco.Replace(s, "\n\n");

        // Remove todas as tags restantes.
        s = TagQualquer.Replace(s, string.Empty);

        // Decode de entidades HTML (&amp; &nbsp; &lt; etc.).
        s = WebUtility.HtmlDecode(s);

        // Normaliza whitespace.
        s = s.Replace("\r\n", "\n");
        s = EspacosExcessivos.Replace(s, " ");
        s = QuebrasExcessivas.Replace(s, "\n\n");

        // Trim final em cada linha (limpa espaços que sobraram entre tag e texto).
        var sb = new StringBuilder(s.Length);
        foreach (var linha in s.Split('\n'))
            sb.AppendLine(linha.Trim());

        return sb.ToString().Trim();
    }
}
