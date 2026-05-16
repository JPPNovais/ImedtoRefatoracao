namespace Imedto.Backend.Domain.Inventario.Cadastros;

/// <summary>
/// Validação de CNPJ (dígitos verificadores). Espelhada em <c>frontend/src/utils/validateCnpj.ts</c>.
/// Mantida no Domain porque é regra de negócio — vai ser usada por Fornecedor e
/// por qualquer outro aggregate futuro que precise de PJ.
/// </summary>
internal static class CnpjValidator
{
    public static string? Normalizar(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? null : digits;
    }

    // Pesos pré-alocados (CA1861: evita realocar arrays a cada chamada).
    private static readonly int[] PesosDv1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] PesosDv2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    public static bool EhValido(string? cnpj)
    {
        var digits = Normalizar(cnpj);
        if (digits is null || digits.Length != 14) return false;
        // Todos iguais (00000000000000, 11111111111111, ...) = inválido
        if (digits.Distinct().Count() == 1) return false;

        int dv1 = CalcularDigito(digits, 12, PesosDv1);
        int dv2 = CalcularDigito(digits, 13, PesosDv2);

        return digits[12] - '0' == dv1 && digits[13] - '0' == dv2;
    }

    private static int CalcularDigito(string digits, int len, int[] pesos)
    {
        int soma = 0;
        for (int i = 0; i < len; i++)
            soma += (digits[i] - '0') * pesos[i];
        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
