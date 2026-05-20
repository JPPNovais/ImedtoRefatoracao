namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Extrai versão texto puro do conteúdo HTML (para snapshot, busca, indexação e
/// pré-visualização sem renderização). Não precisa preservar layout — só semântica:
/// quebra entre blocos, listas em "- ", links em "texto (url)".
/// </summary>
public interface ITermoTextoExtractor
{
    string Extrair(string conteudoHtml);
}
