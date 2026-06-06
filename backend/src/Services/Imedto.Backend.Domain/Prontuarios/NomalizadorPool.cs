using System.Globalization;
using System.Text;

namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Normalização canônica de nomes do pool de variáveis:
/// trim + lower + remoção de diacríticos (acentos).
/// Centraliza a dedup para que CRUD manual e criação automática usem a mesma regra.
/// </summary>
public static class NormalizadorPool
{
    /// <summary>
    /// Retorna o nome normalizado: trim, minúsculo e sem diacríticos.
    /// Ex.: "  Hipertensão  " → "hipertensao"
    /// </summary>
    public static string Normalizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        var s = nome.Trim().ToLowerInvariant();
        var normalizada = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizada.Length);
        foreach (var c in normalizada)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
