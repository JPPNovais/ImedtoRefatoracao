namespace Imedto.Backend.Test.Helpers;

/// <summary>
/// Helper para gerar CPFs validos em testes. Antes do CpfValidator no dominio
/// varios testes usavam sequencias repetidas (000.000.000-00, 111.111.111-11)
/// que agora sao explicitamente invalidas — use os helpers daqui.
/// </summary>
public static class CpfTestData
{
    /// <summary>
    /// CPFs validos pre-calculados (DV correto, sem sequencias repetidas).
    /// Use o indice para garantir variedade entre cenarios paralelos.
    /// </summary>
    public static readonly string[] Validos =
    {
        ComDigitos("123456789"), // index 0
        ComDigitos("111444777"), // 1
        ComDigitos("529982247"), // 2
        ComDigitos("390533447"), // 3
        ComDigitos("987654321"), // 4
        ComDigitos("100000001"), // 5
        ComDigitos("100000002"), // 6
        ComDigitos("100000003"), // 7
        ComDigitos("100000004"), // 8
    };

    /// <summary>
    /// Recebe os 9 primeiros digitos e calcula os 2 verificadores.
    /// Util para gerar CPFs sob demanda (ex: stress, uniqueness por seed).
    /// </summary>
    public static string ComDigitos(string nove)
    {
        if (nove?.Length != 9 || !nove.All(char.IsDigit))
            throw new ArgumentException("Forneca 9 digitos.", nameof(nove));

        var dv1 = CalcularDv(nove, 9);
        var dv2 = CalcularDv(nove + dv1, 10);
        return nove + dv1 + dv2;
    }

    private static int CalcularDv(string digitos, int comprimento)
    {
        var soma = 0;
        var peso = comprimento + 1;
        for (var i = 0; i < comprimento; i++)
            soma += (digitos[i] - '0') * (peso - i);
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
