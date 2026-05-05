namespace Imedto.Backend.SharedKernel.Text;

/// <summary>
/// Helpers genéricos de sanitização de strings — reusados por aggregates do domínio
/// (CPF, CNPJ, telefone, CEP) e pelo CpfValidator. Mantenha aqui apenas funções
/// puras, sem dependência de domínio.
/// </summary>
public static class TextSanitizer
{
    /// <summary>Retorna apenas os dígitos da string informada. Null/empty → "".</summary>
    public static string SomenteDigitos(string valor) =>
        string.IsNullOrEmpty(valor) ? string.Empty : new string(valor.Where(char.IsDigit).ToArray());

    /// <summary>
    /// Retorna o trim de <paramref name="valor"/> ou <c>null</c> se for null/whitespace.
    /// Útil para campos opcionais onde "" deve virar null antes de persistir.
    /// </summary>
    public static string TrimOuNulo(string valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    /// <summary>
    /// Retorna apenas os dígitos de <paramref name="valor"/> ou <c>null</c> se vazio.
    /// Útil para telefone/CPF/CNPJ opcionais.
    /// </summary>
    public static string DigitosOuNulo(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var digitos = SomenteDigitos(valor);
        return digitos.Length == 0 ? null : digitos;
    }
}
