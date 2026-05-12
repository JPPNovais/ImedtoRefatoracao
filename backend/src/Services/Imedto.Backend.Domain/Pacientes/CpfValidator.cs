using Imedto.Backend.SharedKernel.Text;

namespace Imedto.Backend.Domain.Pacientes;

/// <summary>
/// Validador de CPF — algoritmo dos dígitos verificadores (Receita Federal).
/// Aceita string com pontuação ou só dígitos. Rejeita sequências repetidas (ex: 111.111.111-11).
/// </summary>
public static class CpfValidator
{
    /// <summary>
    /// Valida o CPF. Aceita só dígitos ou já formatado. Sequências repetidas
    /// (000.000.000-00 ... 999.999.999-99) sempre passariam no algoritmo de DV
    /// e por isso são explicitamente rejeitadas.
    /// </summary>
    public static bool EhValido(string cpf)
    {
        var digitos = TextSanitizer.SomenteDigitos(cpf);
        if (digitos.Length != 11) return false;
        if (digitos.Distinct().Count() == 1) return false;

        return DigitoVerificador(digitos, 9) == (digitos[9] - '0')
            && DigitoVerificador(digitos, 10) == (digitos[10] - '0');
    }

    /// <summary>
    /// Retorna a razão específica da invalidez do CPF, ou <c>null</c> se for válido.
    /// Mensagens granulares ajudam o usuário a corrigir o input — "CPF inválido"
    /// genérico não distingue se o problema é quantidade de dígitos, sequência
    /// repetida ou dígito verificador.
    /// </summary>
    public static string? RazaoInvalidez(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return "CPF é obrigatório.";
        var digitos = TextSanitizer.SomenteDigitos(cpf);
        if (digitos.Length != 11)
            return "CPF deve conter 11 dígitos.";
        if (digitos.Distinct().Count() == 1)
            return "CPF inválido (sequência repetida não é aceita).";
        if (DigitoVerificador(digitos, 9) != (digitos[9] - '0')
            || DigitoVerificador(digitos, 10) != (digitos[10] - '0'))
            return "CPF inválido (dígito verificador não confere).";
        return null;
    }

    private static int DigitoVerificador(string digitos, int comprimento)
    {
        var soma = 0;
        var peso = comprimento + 1;
        for (var i = 0; i < comprimento; i++)
            soma += (digitos[i] - '0') * (peso - i);
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
