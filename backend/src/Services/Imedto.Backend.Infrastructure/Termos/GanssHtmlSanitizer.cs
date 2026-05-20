using Ganss.Xss;
using Imedto.Backend.Domain.Termos;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Implementação de <see cref="ITermoHtmlSanitizer"/> usando
/// <see cref="HtmlSanitizer"/> (Ganss.Xss) com whitelist restrita.
///
/// O <see cref="HtmlSanitizer"/> é thread-safe quando suas coleções não são
/// modificadas após o setup — registramos como singleton.
/// </summary>
public sealed class GanssHtmlSanitizer : ITermoHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public GanssHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // Whitelist completa de tags — descarta o resto.
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "strong", "em", "u", "ul", "ol", "li",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "blockquote", "hr", "span", "div", "a"
        })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        // Atributos — só href em <a> (já é default; reforçamos limpando o resto).
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");

        // Esquemas seguros para href.
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");

        // Sem CSS inline — block total.
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowDataAttributes = false;
    }

    public string Sanitizar(string conteudoHtml)
    {
        if (string.IsNullOrWhiteSpace(conteudoHtml)) return string.Empty;
        return _sanitizer.Sanitize(conteudoHtml);
    }
}
