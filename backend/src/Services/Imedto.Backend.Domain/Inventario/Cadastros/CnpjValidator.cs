namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Validação de CNPJ (dígitos verificadores). Espelhada em <c>frontend/src/utils/validateCnpj.ts</c>.
/// Mantida no Domain porque é regra de negócio — vai ser usada por Fornecedor e
/// por qualquer outro aggregate futuro que precise de PJ.
///
/// Suporta o formato alfanumérico da IN RFB 2.229/2024 (vigência 06/07/2026):
/// as 12 primeiras posições aceitam [A-Z0-9]; as 2 últimas (dígitos verificadores)
/// continuam numéricas. O valor de cada caractere no cálculo do DV é (ASCII - 48),
/// o que produz 0-9 para '0'-'9' e 17-42 para 'A'-'Z'.
///
/// IMPORTANTE: esta normalização é DEDICADA ao CNPJ. NÃO use nem altere
/// <see cref="Imedto.Backend.SharedKernel.Text.TextSanitizer.SomenteDigitos"/>,
/// que é genérico (CPF, telefone, CEP — só dígitos).
/// </summary>
public static class CnpjValidator
{
    /// <summary>
    /// Normaliza o CNPJ para a forma canônica: preserva [A-Z0-9], aplica ToUpperInvariant,
    /// remove qualquer outro caractere (pontos, barra, hífen, espaços, etc.).
    /// Retorna null para entrada vazia/nula ou quando não restam caracteres válidos.
    /// </summary>
    public static string? Normalizar(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var upper = cnpj.ToUpperInvariant();
        var canonical = new string(upper.Where(c => char.IsAsciiLetterOrDigit(c)).ToArray());
        return string.IsNullOrEmpty(canonical) ? null : canonical;
    }

    // Pesos pré-alocados (CA1861: evita realocar arrays a cada chamada).
    private static readonly int[] PesosDv1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] PesosDv2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    public static bool EhValido(string? cnpj)
    {
        var digits = Normalizar(cnpj);
        if (digits is null || digits.Length != 14) return false;

        // As 12 primeiras posições devem ser [A-Z0-9]; as 2 últimas (DV) apenas [0-9].
        for (int i = 0; i < 12; i++)
            if (!char.IsAsciiLetterOrDigit(digits[i])) return false;
        if (!char.IsAsciiDigit(digits[12]) || !char.IsAsciiDigit(digits[13])) return false;

        // Todos iguais (00000000000000, AAAAAAAAAAAAAA, ...) = inválido
        if (digits.Distinct().Count() == 1) return false;

        int dv1 = CalcularDigito(digits, 12, PesosDv1);
        int dv2 = CalcularDigito(digits, 13, PesosDv2);

        // DV é numérico: compara com valor numérico do char (ASCII - 48)
        return digits[12] - '0' == dv1 && digits[13] - '0' == dv2;
    }

    /// <summary>
    /// Calcula um dígito verificador do CNPJ.
    /// Valor de cada caractere = (ASCII - 48): '0'-'9' → 0-9; 'A'-'Z' → 17-42.
    /// </summary>
    private static int CalcularDigito(string digits, int len, int[] pesos)
    {
        int soma = 0;
        for (int i = 0; i < len; i++)
            soma += (digits[i] - '0') * pesos[i];
        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
