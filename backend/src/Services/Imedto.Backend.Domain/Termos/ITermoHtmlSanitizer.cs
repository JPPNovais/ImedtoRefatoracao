namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Sanitiza HTML do termo antes de persistir/emitir. Whitelist:
/// <list type="bullet">
///   <item>Tags: <c>p, br, strong, em, u, ul, ol, li, h1..h6, blockquote, hr, span, div, a</c>.</item>
///   <item>Atributo permitido: <c>href</c> em <c>&lt;a&gt;</c>, com esquema <c>http</c>/<c>https</c>.</item>
///   <item>Remove: <c>script, iframe, style, link, meta, on*</c>, atributos <c>style</c>/<c>class</c>.</item>
/// </list>
/// Sempre retorna string segura — qualquer conteúdo malicioso é silenciosamente
/// removido. A regra de negócio (campo obrigatório, tamanho, etc.) é responsabilidade
/// do aggregate <see cref="TermoModelo"/>.
/// </summary>
public interface ITermoHtmlSanitizer
{
    string Sanitizar(string conteudoHtml);
}
